# Icons

`Icons.xaml` contains JuanTool's green square with a black J and the vector settings gear. The J outline comes from Segoe UI Bold. `juantool.ico` includes sizes from 16 to 256 pixels for the executable, Windows title bars, shortcuts and notification area. To regenerate it after changing the vector artwork, run `powershell -ExecutionPolicy Bypass -File scripts/generate-app-icon.ps1` from the repository root.

## Service icons

These official images are bundled as WPF resources so the search buttons work offline and do not request images while typing. The original files are unmodified and displayed at their original aspect ratios.

- `google.png`: Google G, downloaded from [Google's asset server](https://www.gstatic.com/images/branding/product/2x/googleg_48dp.png). See the [Google Brand Resource Center](https://about.google/brand-resource-center/).
- `chatgpt.png`: ChatGPT app icon, linked from [OpenAI's official icon identification article](https://help.openai.com/en/articles/7905742-what-does-the-official-chatgpt-ios-app-icon-look-like), downloaded from [the article's image asset](https://images.ctfassets.net/j22is2dtoxu1/intercom-img-d177d076c9a5453052925143/49d5d812b0a6fcc20a14faa8c629d9fb/icon-ios-1024_401x.png). See [OpenAI's brand guidelines](https://openai.com/brand/).

Retrieved September 21, 2026. Google and its logo belong to Google LLC. ChatGPT and its logo belong to OpenAI. These third-party trademarks are not covered by JuanTool's MIT license; they identify the destinations opened by the buttons and do not imply affiliation or endorsement.
