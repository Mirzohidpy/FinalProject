#!/usr/bin/env python3
"""Brain Citizen Academy dev backend — Python stdlib only, no installs.

Mirrors web/server.js so it can be run interchangeably:
    python3 server.py            # serves http://localhost:8080
    PORT=3000 python3 server.py  # custom port

Static files live in web/public/. Replace with proper backend (Firebase /
Express on Cloud Run / etc.) for production. State is in-memory.
"""

from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Optional
from urllib.parse import urlparse, parse_qs
from pathlib import Path
import json
import mimetypes
import os
import sys
import time

ROOT     = Path(__file__).resolve().parent
PUBLIC   = ROOT / "public"
BUILD    = PUBLIC / "build"
PORT     = int(os.environ.get("PORT", "8080"))

GAMES = [
    {"gameId": "true-false-news", "title": "True or False News", "category": "Civics",    "scene": "TrueFalseNews"},
    {"gameId": "flag-quiz",       "title": "Flag Quiz",          "category": "Geography", "scene": "FlagQuiz"},
    {"gameId": "word-search",     "title": "Word Search",        "category": "Language",  "scene": "WordSearch"},
    {"gameId": "emotion-id",      "title": "Emotion ID",         "category": "Social",    "scene": "EmotionID"},
    {"gameId": "math-sprint",     "title": "Math Sprint",        "category": "Math",      "scene": "MathSprint"},
    {"gameId": "civic-quiz",      "title": "Quiz Showdown",      "category": "Civics",    "scene": "CivicQuiz"},
    {"gameId": "maze-runner",     "title": "Maze Runner",        "category": "Logic",     "scene": "MazeRunner"},
    {"gameId": "timeline-sort",   "title": "Timeline Sort",      "category": "History",   "scene": "TimelineSort"},
    {"gameId": "doppi-facts",     "title": "Doppi Facts",        "category": "Civics",    "scene": "DoppiFacts"},
    {"gameId": "memory-match",    "title": "Memory Match",       "category": "Memory",    "scene": "MemoryMatch"},
]
SEED_LEADERBOARD = [
    {"name": "Aziza",   "points": 320},
    {"name": "Bekzod",  "points": 285},
    {"name": "Dilnoza", "points": 240},
    {"name": "Jasur",   "points": 210},
    {"name": "Madina",  "points": 180},
]

scores_by_game = {g["gameId"]: [] for g in GAMES}
profiles = {}

COINS_PER_POINT  = 10
DAILY_POINTS_CAP = 200

mimetypes.add_type("application/wasm",         ".wasm")
mimetypes.add_type("application/octet-stream", ".data")
mimetypes.add_type("application/octet-stream", ".unityweb")


def get_or_create_profile(player_id: str, name: Optional[str]) -> dict:
    p = profiles.get(player_id)
    if p is None:
        p = {"playerId": player_id, "name": name or "Anonymous",
             "coins": 0, "points": 0, "perGame": {}}
        profiles[player_id] = p
    elif name and p["name"] == "Anonymous":
        p["name"] = name
    return p


class Handler(BaseHTTPRequestHandler):
    server_version = "BrainCitizenPortal/0.1"

    def _send_json(self, code: int, payload: dict) -> None:
        body = json.dumps(payload).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)

    def _serve_file(self, fs_path: Path) -> None:
        if not fs_path.exists() or not fs_path.is_file():
            self.send_error(404, "Not found")
            return

        ctype, _ = mimetypes.guess_type(str(fs_path))
        ctype = ctype or "application/octet-stream"

        encoding = None
        s = str(fs_path)
        if s.endswith(".gz"):
            encoding = "gzip"
            inner, _ = mimetypes.guess_type(s[:-3])
            ctype = inner or ctype
        elif s.endswith(".br"):
            encoding = "br"
            inner, _ = mimetypes.guess_type(s[:-3])
            ctype = inner or ctype

        size = fs_path.stat().st_size
        self.send_response(200)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(size))
        if encoding:
            self.send_header("Content-Encoding", encoding)
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        self.end_headers()
        with fs_path.open("rb") as fh:
            self.wfile.write(fh.read())

    def _resolve_static(self, url_path: str) -> Optional[Path]:
        rel = url_path.lstrip("/") or "index.html"
        target = (PUBLIC / rel).resolve()
        if not str(target).startswith(str(PUBLIC)):
            return None
        if target.is_dir():
            target = target / "index.html"
        return target

    def log_message(self, fmt, *args):
        sys.stderr.write("%s [%s] %s\n" % (self.address_string(), self.log_date_time_string(), fmt % args))

    def do_GET(self):  # noqa: N802
        url = urlparse(self.path)
        path = url.path

        if path == "/api/health":
            return self._send_json(200, {"ok": True, "ts": int(time.time() * 1000)})
        if path == "/api/games":
            return self._send_json(200, {"games": GAMES})
        if path == "/api/leaderboard":
            qs     = parse_qs(url.query)
            gameId = (qs.get("gameId") or [""])[0]
            if gameId and gameId in scores_by_game:
                top = sorted(scores_by_game[gameId], key=lambda s: -s["score"])[:50]
                rows = [{"name": s["name"], "points": s["score"]} for s in top]
                return self._send_json(200, {"scope": gameId, "leaderboard": rows})
            top_global = sorted(profiles.values(), key=lambda p: -p["points"])[:50]
            rows = [{"name": p["name"], "points": p["points"]} for p in top_global]
            return self._send_json(200, {"scope": "global", "leaderboard": rows or SEED_LEADERBOARD})
        if path == "/api/profile":
            qs        = parse_qs(url.query)
            player_id = (qs.get("playerId") or [""])[0]
            if not player_id:
                return self._send_json(400, {"error": "playerId is required."})
            p = profiles.get(player_id)
            if not p:
                return self._send_json(404, {"error": "No profile yet for that playerId."})
            return self._send_json(200, {"profile": p})

        target = self._resolve_static(path)
        if target is None:
            return self.send_error(403, "Forbidden")
        return self._serve_file(target)

    def do_HEAD(self):  # noqa: N802
        url = urlparse(self.path)
        path = url.path
        if path.startswith("/api/"):
            known = {"/api/health", "/api/games", "/api/leaderboard", "/api/profile"}
            if path in known:
                self.send_response(200)
                self.send_header("Content-Type", "application/json; charset=utf-8")
                self.end_headers()
            else:
                self.send_error(404, "Not found")
            return
        target = self._resolve_static(path)
        if target is None or not target.exists() or not target.is_file():
            return self.send_error(404, "Not found")
        ctype, _ = mimetypes.guess_type(str(target))
        self.send_response(200)
        self.send_header("Content-Type", ctype or "application/octet-stream")
        self.send_header("Content-Length", str(target.stat().st_size))
        self.end_headers()

    def do_POST(self):  # noqa: N802
        url = urlparse(self.path)
        if url.path != "/api/score":
            return self.send_error(404, "Not found")

        length = int(self.headers.get("Content-Length") or 0)
        if length <= 0 or length > 32 * 1024:
            return self._send_json(400, {"error": "Body required (≤ 32 KiB)."})
        try:
            body = json.loads(self.rfile.read(length).decode("utf-8"))
        except Exception:
            return self._send_json(400, {"error": "Invalid JSON."})

        game_id = body.get("gameId")
        score   = body.get("score")
        if game_id not in scores_by_game:
            return self._send_json(400, {"error": "Unknown gameId. Allowed: " + ", ".join(scores_by_game.keys())})
        try:
            num_score = float(score)
        except (TypeError, ValueError):
            return self._send_json(400, {"error": "Score must be a number."})
        if not (0 <= num_score <= 1_000_000):
            return self._send_json(400, {"error": "Score must be between 0 and 1,000,000."})

        player_id = str(body.get("playerId") or f"anon-{int(time.time() * 1000)}")[:64]
        profile   = get_or_create_profile(player_id, body.get("name"))

        scores_by_game[game_id].append({
            "playerId": player_id, "name": profile["name"],
            "score": num_score, "ts": int(time.time() * 1000),
        })

        coins_earned = min(num_score, 1000)
        profile["coins"] += coins_earned
        points_to_award = int(profile["coins"] // COINS_PER_POINT)
        profile["coins"] -= points_to_award * COINS_PER_POINT
        profile["points"] = min(DAILY_POINTS_CAP * 30, profile["points"] + points_to_award)

        per = profile["perGame"].get(game_id, {"best": 0, "plays": 0})
        per["best"] = max(per["best"], num_score)
        per["plays"] += 1
        profile["perGame"][game_id] = per

        return self._send_json(200, {"ok": True, "profile": profile})


def main():
    build_index = BUILD / "braincitizen" / "index.html"
    print(f"Brain Citizen Academy portal running:  http://localhost:{PORT}")
    print(f"  /                   → landing page")
    print(f"  /play.html          → Unity WebGL")
    print(f"  /api/health         → backend liveness")
    print(f"  /api/games          → game catalog")
    print(f"  /api/leaderboard    → top players (global, or ?gameId=flag-quiz)")
    print(f"  /api/score          → POST {{ gameId, score, playerId, name }}")
    print(f"  /api/profile        → GET ?playerId=...")
    if build_index.exists():
        print(f"  /build/braincitizen/index.html → Unity WebGL build (found)")
    else:
        print(f"  ⚠ Unity WebGL build missing. In Unity: BrainCitizen → Build WebGL.")
    print()

    httpd = ThreadingHTTPServer(("0.0.0.0", PORT), Handler)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down.")


if __name__ == "__main__":
    main()
