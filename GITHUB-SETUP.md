# GitHub repository setup

The local repository already has a `main` branch and an initial commit. Build outputs, cached tools, and personal Chrome data are excluded.

1. Open https://github.com/new and sign in as **Duohewater**.
2. Set the repository name to **JuanTool** and choose the visibility you want.
3. Leave **Add a README**, **Add .gitignore**, and **Choose a license** unchecked (the local project already contains its files).
4. Click **Create repository**.
5. Open PowerShell and run:

```powershell
cd 'C:\path\to\JuanTool'
git remote add origin https://github.com/Duohewater/JuanTool.git
git push -u origin main
```

Replace `C:\path\to\JuanTool` with the folder where you cloned the repository. Complete the browser sign-in if Git asks. If you create the repository under a different account, replace `Duohewater` in the remote URL.

After the push, the GitHub Actions workflow builds a standalone Windows app. Its download is available under **Actions → Build Windows app → the run → Artifacts** once the workflow succeeds.
