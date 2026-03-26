// This file is a helper to remind devs what data to fill in.
// All actual headline assets are ScriptableObjects created in the Unity Editor.
// See Assets/Data/Headlines/ for the .asset files.
//
// To create a new headline:
//   Right-click in Project window > Create > BrainCitizen > Headline
//
// Required fields per headline:
//   headline      : the news headline text
//   isReal        : true = real news, false = fake news
//   explanation   : fact shown after the answer
//   sourceHint    : e.g. "Source: WHO, 2023" or "No credible source found"
//   category      : Politics / Health / Science / WorldEvents
