---
name: 🌐 Language Submission
title: "🌍 Language Submission"
description: Submit a language file to be supported by Streamerfy
labels: [language, contribution]
assignees: WTFBlaze
body:
  - type: input
    id: language
    attributes:
      label: 🌐 Language
      description: What language are you submitting a file for?
      placeholder: e.g., German, Spanish, French
    validations:
      required: true

  - type: textarea
    id: lang_json
    attributes:
      label: 📝 Language File Contents
      description: Paste your full language file JSON here.
      placeholder: Enter the full language JSON file...
      render: json
    validations:
      required: true
---
