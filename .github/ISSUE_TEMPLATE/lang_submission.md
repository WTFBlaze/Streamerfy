name: 🌐 Language Submission
description: Submit a language file to be supported by Streamerfy
title: "[Lang] New Language Submission"
labels: ["language", "enhancement"]
assignees:
  - WTFBlaze

body:
  - type: input
    id: language
    attributes:
      label: 🌍 Language Name
      description: What language are you submitting? (e.g., French, German, Japanese)
      placeholder: e.g., English
    validations:
      required: true

  - type: textarea
    id: file_contents
    attributes:
      label: 📄 Language File Contents
      description: Paste the entire language JSON file content here.
      placeholder: "{ \"version\": \"1.0.0\", \"Tabs_Logs\": \"Logs\" }"
      render: json
    validations:
      required: true
