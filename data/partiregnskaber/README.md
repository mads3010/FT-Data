# Party accounts (partiregnskaber)

Drop the combined annual PDFs published by Folketinget here, e.g. `partiregnskaber_2023.pdf`, then run:

```bash
dotnet run --project src/FolketingetVotes.Ingest -- import-party-accounts data/partiregnskaber/partiregnskaber_2023.pdf
```

Where to get them: ft.dk › Organisation › Folketingets administration › Folketingets tal og regnskaber › Partierne.
The site is behind a bot challenge, so the download has to be done in a browser. PDFs are ignored by git.
