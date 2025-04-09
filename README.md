# 🎵 Streamerfy

**Let your Twitch chat take over your Spotify queue — safely, securely, and with style.**

Streamerfy lets streamers connect their Twitch chat to Spotify, allowing viewers to request songs using simple commands like `!queue`. Have no fear with blacklist controls, explicit content filters, and real-time queuing — it’s a must-have for music-loving streamers.  

Made with ❤️ for streamers who want more from their community.

---

## ✨ Features

- ✅ **Live Twitch chat integration**
- 🎧 **Real-time Spotify queueing**
- 🚫 **Blacklist support** (track & artist level)
- 🔞 **Explicit content filtering**
- 🖥️ **Clean, dark-themed User Interface**
- 📦 **One-click standalone `.exe` build**

---

## 🖼 Preview

![Streamerfy UI Preview](https://github.com/WTFBlaze/Streamerfy/blob/master/Images/Showcase.png?raw=true) <!-- You can replace this with a real link or remove it -->

---

## 🛠 Requirements

- Windows 10 or 11
- Spotify Premium account
- Twitch account with OAuth token (can be streamer account or alt account)
- [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

---

## 🚀 Getting Started

### 🔹 1. Download
Download the latest release from the [Releases](https://github.com/WTFBlaze/Streamerfy/releases) page.

### 🔹 2. Launch
Run `Streamerfy.exe`.

### 🔹 3. Configure Settings
- Enter your bot username and OAuth token
- Set your Twitch channel and [Spotify App Credentials](https://developer.spotify.com/dashboard)
- Enable "Auto-connect" if desired

### 🔹 4. Commands (default)
| Command        | Description                         |
|----------------|-------------------------------------|
| `!queue <url>`  | Adds a Spotify track to the queue   |
| `!blacklist song <url>` | Mods can blacklist a song   |
| `!unblacklist song <url>` | Mods can unblacklist a song |
| `!blacklist artist <url>` | Mods can blacklist an artist |
| `!unblacklist artist <url>` | Mods can unblacklist an artist |
| `!rban <username>` | Mods can ban users from using Streamerfy commands |
| `!runban <username>` | Mods can unban users from using Streamerfy commands |

---

## 🧠 Advanced Features

- 🧩 **Global blacklist** support via remote list (For those absolute must blacklist songs that are not content creator friendly)
- 🔄 **Auto-connect** on launch
- 🎨 Toggleable UI sections to hide sensitive info

---

## 🛡️ Safe & Streamer Friendly

Streamerfy runs **entirely on your machine**, with no cloud-based commands, no server in the middle, and all logs are local. You're in full control.

---

## 🆓 Always Free & Open Source
Streamerfy is — and always will be — **100% free** and **open source**.  
No paywalls, no subscriptions, no shady unlocks. Just good software, built for the community.

> 💖 If you enjoy using Streamerfy and want to support development, you can [buy me a Ko-fi](https://ko-fi.com/wtfblaze)!

## 👨‍💻 Dev & Build

To build the project yourself:

```bash
git clone https://github.com/WTFBlaze/Streamerfy.git
cd Streamerfy
build-release.bat
