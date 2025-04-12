# 🎵 Streamerfy

[![Latest Version](https://img.shields.io/github/v/tag/WTFBlaze/Streamerfy?style=flat-square)](https://github.com/WTFBlaze/Streamerfy/releases)
[![Platform](https://img.shields.io/badge/platform-windows-blue?style=flat-square)](#requirements)
[![License](https://img.shields.io/github/license/WTFBlaze/Streamerfy?style=flat-square)](LICENSE)

**Let your Twitch chat take over your Spotify queue — safely, securely, and with style.**

Streamerfy connects your Twitch chat to your Spotify queue, letting viewers request songs using simple commands like `!queue`. With blacklist controls, explicit content filtering, and Twitch moderation tools — it's built for streamers who want their community involved without chaos.

Made with ❤️ for creators who want more from their music streams.

---

## ✨ Key Features

- 🎧 **Spotify Playback Control** – Queue songs via Twitch chat
- 💬 **Custom Commands** – All chat commands are customizable
- 🚫 **Blacklist Management** – Block specific songs or artists
- 🔞 **Explicit Filter** – Toggle on/off based on your vibe
- 🧩 **Global Blacklist** – Community-powered banned songs
- 🎥 **OBS & TikTok Now Playing Overlay** – Auto-updating HTML overlay for your stream
- 🧼 **Local Only** – No servers, no tracking. Your data stays with you.
- 🔊 **Multi-Language** - Supports language files for native language translation
- 🔐 **Permissions Customization** - Change who can run certain commands
---

## 🖼 Preview

![Streamerfy UI Preview](https://github.com/WTFBlaze/Streamerfy/blob/master/Images/Showcase.png?raw=true)

---

## 🆓 Always Free & Open Source

Streamerfy is — and always will be — **100% free** and **open source**.  
No paywalls. No premium unlocks. Just good software made for streamers.

> 💖 Like my work? [Buy me a Ko-fi](https://ko-fi.com/notblazelol) to support development.

---

## 🛠 Requirements

- Windows 10 or 11
- Spotify Premium account
- Twitch account with OAuth token
- [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-6.0.36-windows-x64-installer)

---

## 🚀 Quick Start

1. **[Download the latest release](https://github.com/WTFBlaze/Streamerfy/releases)**
2. **Run `StreamerfyInstallervX.X.X.exe`**
3. **Open `Streamerfy` from your Windows Search Bar**
4. **Configure settings:** Twitch account, OAuth token, Spotify app credentials
5. **Connect and start queuing songs!**

> ✅ Full setup walkthroughs, including OBS integration, are now in the [📚 Wiki](https://github.com/WTFBlaze/Streamerfy/wiki)

---

## 📝 To-Do List

Planned features and future improvements:

- [x] Spotify integration
- [x] Twitch chat command handling
- [x] Track & artist blacklist support
- [x] Explicit content filtering
- [x] Global blacklist
- [x] Standalone `.exe` build
- [x] NowPlaying.html OBS overlay
- [x] Auto-update checker
- [x] Playback history
- [x] Language localizations
- [x] Permission roles (VIPs, Subs, Mods)
- [ ] Command cooldowns / spam protection
- [ ] Discord webhook integration
- [ ] Twitch Channel Point Redeems
- [ ] TikTok Live integration (Widget Supports TikTok Live Studio but TikTok Chat Requests are still to be added)

> Got an idea? [Open an issue](https://github.com/WTFBlaze/Streamerfy/issues) or leave feedback in [Discussions](https://github.com/WTFBlaze/Streamerfy/discussions)

---
## Language Translation Credits

Thank you to everyone contributing to the Language Files 💙

| Language        | Authors      |
| ------------- | -------------: |
| English | [WTFBlaze](https://github.com/WTFBlaze) |
| Portuguese-Brazilian | [Caroline Versiani](https://github.com/yoitscarolinee) |

---

## 👨‍💻 Building from Source

```bash
git clone https://github.com/WTFBlaze/Streamerfy.git
cd Streamerfy
build-release.bat
