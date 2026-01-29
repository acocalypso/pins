# First Time setup

Download the Pins image from <link>
Flash it to sdcard / internal emmc 32GB min. space required
128GB recommended

Use tool of your choice to flash it

## Setup
Once flashed you can power on the pi, attach lan cable to update the packages.

ssh username: pi password pins
`sudo apt update && sudo apt upgrade`

Confirm packages for installation.

The pi will create a hotspot called pins_xxxxx

password touchnstars

## Network information for hotspot
 10.42.0.1/24

 Interface is reachable by Web and TNS App (use beta channel for now)

## In order to use it
You can connect enable the pins modul for updates and enable samba share.

Once Mounted on your pc you can copy your sequences to the pi.

Use VNC (Plugin) for setting up PHD2