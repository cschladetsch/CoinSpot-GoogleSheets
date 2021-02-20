# CoinSpot/Google Sheets Updater

[![CodeFactor](https://www.codefactor.io/repository/github/cschladetsch/CoinSpot-GoogleSheets/badge)](https://www.codefactor.io/repository/github/cschladetsch/CoinSpot-GoogleSheets) [![License](https://img.shields.io/github/license/cschladetsch/CoinSpot-GoogleSheets.svg?label=License&maxAge=86400)](./LICENSE) [![Release](https://img.shields.io/github/release/cschladetsch/CoinSpot-GoogleSheets.svg?label=Release&maxAge=60)](https://github.com/cschladetsch/CoinSpot-GoogleSheets/releases/latest)

Watch the intro [video](https://www.youtube.com/watch?v=csmDEE-CY3M).

This simple console application reads values from your *CoinSpot* account, and writes the total values of your holdings to a *GoogleSheet* spread sheet.

Of course you will need to supply your own API keys and secrets for the APIs used.

It's faster to type 'g' or 'b' into the console of this app, rather than opening a new tab, logging in, and checking things.

This app will also automatically update fields in a Google Sheet of yours, if you like that sort of  thing. This requires extra setup of course, but it's not hard.

Most people that invest in crypto-currencies have their own spreadsheets. This app will give you basic use cases, and also provide a basis for more elaborate automation if you wish.

*Note*: Balances will **not** include your unused account funds on CoinSpot. This is intentional. The same is true for 'total spent', though I'll need fully automate this. This is all a bit more complicated than I first suspected.

## Example Screenshot
This is using an alt account setup, but shows the basics of what you can expect.

Note that when you use the 'up' command, if you have the Spreadsheet open in a browser window, it will instantly update as well.

You can use this framework to connect your inputs from CoinSpot to your outputs/analysis in Google Sheets.

An example session using an alt account of mine:

![Sample Session](Resources/Demo.png)

NOTE: I don't update the image with every change the app, so it may differ from what you see when you build it. Any drastic changes, I'll update the image.

## Setup
Most configuration is stored in `App.config`. Start with `App.config.example` and rename it to `App.config`. Add all the required keys and secrets.

You will need a CoinSpot account. The default is CoinSpot Australia.

You will also need a Google account and at least on Google Sheet if you wish to use the auto-update methods. You can use the app without this of course.

## External references
These are the required references:

* Google Api. Install via nuget
* Google Sheets v4. Install via nuget package console `code example. Install via nuget package console `code example

## CoinSpot API
Login to CoinSpot and get an API key and secret. Add them to `App.config`.

## GoogleSheets API
Get your Google API credentials from [this page](https://developers.google.com/sheets/api/quickstart/dotnet). Put it in the folder along side the executable. Add your spreadsheet id to `App.config`.

## Usage
It's a simple console application, which starts by printing the help screen.

Follow the dots.

## Automatic Updating
In the `App.Config` file, you can change the `updateTimerMinutes` setting to automatically update your spreadsheet every N seconds. If this is 0, no auto-updates will be made.

### Using a Raspberry Pi

You don't want to keep your desktop on 24/7 just to write updates to your spreadsheet. But if you have a Raspberry Pi, you can use that to send updates at a very low power consumption that costs a few cents/day in power to run.

#### Making it easier to login to your _pi_

Append the contents of your `~/.ssh/rsa_pub.rsa` file to the `~/.ssh/known_hosts`  file on your pi.

Then you can use `ssh pi@yourpiname` to quickly get into your _pi_ without having to type your password.

### Setup on Pi

* Get everything working locally as you wish
* `ssh` into the pi
* Set your real locale with `sudo dpkg-reconfigure tzdata`
* Install [mono](https://linuxize.com/post/how-to-install-mono-on-ubuntu-18-04/) on the pi
* Logout from the pi
* Use `scp -rp [src] [dest]` to copy the files to the pi. You need to copy recursively as there are sub-folders for the token
  * See `update-pi` script for example
  * _Note_ that you will need the target folder to exist on the pi
* Login to the pi
* Use [screen](https://linuxize.com/post/how-to-use-linux-screen/) to be able to make detachable sessions. Run `screen`
* Start a new *screen* session with \^A\^C
* Change the _updateTimerPeriod_ element in *App.Config* to set the number of minute between updates. The default is zero
* Run *CoinSpotUpdater.exe*
* Detach the process with \^A\^D

You can now close the `ssh` window to the pi. The CoinSpotUpdater process will still keep running on the pi, even if you turn off your desktop.

Later, `ssh` back into the pi and type `screen -ls` to find the session you want then `screen -r ###` where ### is the number output in the `screen -ls` command:

![pi-screen](Resources/pi-screen.png)

In this case, you would say `screen -r 1021`.

You are now back controlling the *CoinSpotUpdater*.

There are probably easier ways to do this, but I was already familiar with `scp` and `screen` so that's how I did it. Feel free to leave feedback on better ways!

## Help

Raise an issue on *GitHub* if you have any questions or bug reports, or [email me](mailto:christian@schladetsch.com) directly. Happy to help set you up.

