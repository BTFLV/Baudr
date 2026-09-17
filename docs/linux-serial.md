# Linux Serial Port Configuration & Troubleshooting

On Linux distributions (Ubuntu, Debian, Fedora, Arch, openSUSE), serial ports (`/dev/ttyUSB*`, `/dev/ttyACM*`, `/dev/ttyS*`) are restricted to root and members of the `dialout` or `uucp` groups by default.

Follow the steps below to grant your user account access to serial devices without running Baudr as `sudo`.

---

## 1. Add User to the `dialout` Group

### Ubuntu / Debian / Linux Mint:
```bash
sudo usermod -aG dialout $USER
```

### Fedora / RHEL / CentOS:
```bash
sudo usermod -aG dialout $USER
```

### Arch Linux / Manjaro:
```bash
sudo usermod -aG uucp $USER
```

> [!IMPORTANT]
> Group changes require logging out and logging back in, or running `newgrp dialout` in the active terminal session.
> You can verify your active group memberships with:
> ```bash
> groups
> ```

---

## 2. Dealing with ModemManager

On some desktop Linux distributions (especially Ubuntu), the `ModemManager` daemon automatically opens any newly connected USB serial converter to test if it is a cellular/GSM modem.

This causes two common symptoms:
1. Baudr reports `Access to the port is denied` for the first 10-20 seconds after plugging in a microcontroller or FTDI cable.
2. The initial boot banner or firmware output sent by the microcontroller is consumed by ModemManager before Baudr connects.

### Solution A: Disable ModemManager (Recommended if you do not use a 4G/5G USB modem)
```bash
sudo systemctl stop ModemManager
sudo systemctl disable ModemManager
```

### Solution B: Whitelist Device via udev Rule
If you need ModemManager for mobile broadband, create a udev rule to ignore development boards:

Create `/etc/udev/rules.d/99-baudr-serial.rules`:
```udev
# Ignore FTDI, CP210x, and CH340 in ModemManager
SUBSYSTEMS=="usb", ATTRS{idVendor}=="0403", ENV{ID_MM_DEVICE_IGNORE}="1"
SUBSYSTEMS=="usb", ATTRS{idVendor}=="10c4", ENV{ID_MM_DEVICE_IGNORE}="1"
SUBSYSTEMS=="usb", ATTRS{idVendor}=="1a86", ENV{ID_MM_DEVICE_IGNORE}="1"
SUBSYSTEMS=="usb", ATTRS{idVendor}=="2e8a", ENV{ID_MM_DEVICE_IGNORE}="1" # Raspberry Pi Pico
SUBSYSTEMS=="usb", ATTRS{idVendor}=="2341", ENV{ID_MM_DEVICE_IGNORE}="1" # Arduino
SUBSYSTEMS=="usb", ATTRS{idVendor}=="303a", ENV{ID_MM_DEVICE_IGNORE}="1" # Espressif ESP32
```

Reload udev rules:
```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

---

## 3. Persistent Port Symlinks with udev

If you use multiple serial adapters whose device node names (`/dev/ttyUSB0`, `/dev/ttyUSB1`) change across reboots, you can assign stable symlinks using udev:

Example `/etc/udev/rules.d/99-usb-serial-symlinks.rules`:
```udev
SUBSYSTEM=="tty", ATTRS{idVendor}=="0403", ATTRS{idProduct}=="6001", ATTRS{serial}=="FT123456", SYMLINK+="ttyUSB_ROBOT_ARM", MODE="0666"
```
Baudr automatically enumerates all `/dev/tty*` nodes and recognizes custom symlinks.

