# GoConsoleOS Android App

A lightweight Android client for the GoConsoleOS Account Center. It wraps the console's
web portal (`index.html` + `acc.js` + `acc.css`) in a WebView and connects to a running
GoConsoleOS console over your local network.

## Build

Prerequisites: .NET 9 SDK with the `maui-android` workload and the Android SDK.

```
dotnet publish src\GoConsoleOS.Mobile\GoConsoleOS.Mobile.csproj -f net9.0-android -c Release
```

The signed APK is written to
`src\GoConsoleOS.Mobile\bin\Release\net9.0-android\com.gostudios.goconsoleos-Signed.apk`
and copied to `mobile\GoConsoleOS-Android.apk`.

## Install

1. Copy `GoConsoleOS-Android.apk` to the phone and open it (allow install from unknown sources).
2. Open the app and enter your console's IP address (port defaults to 39210).
3. Sign in with your GoConsoleOS account and manage devices, the map, wallet, Game Pass
   subscriptions and gift cards from anywhere on your network.

## What it can do

- Sign in / create GoConsoleOS accounts
- Profile, devices, console map, security (2FA), wallet
- GoConsole Game Pass subscriptions (Pro / Plus / Premium / Ultimate)
- Gift card redemption and generation
- Friends and recent activity
- GoAI assistant

## Limitations

- Requires a GoConsoleOS console on your LAN (port 39210) — it is a remote client,
  not a full console emulator. The WPF shell itself cannot run on Android.