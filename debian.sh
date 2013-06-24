#!/bin/bash

xbuild || exit 1

if [ -d /tmp/pidgeonsv_deb ];then
    echo "Clean /tmp/pidgeonsv before"
    exit 1
fi

cp -r installers /tmp/pidgeonsv_deb
cp bin/Debug/pidgeon-sv.exe /tmp/pidgeonsv_deb/usr/share/pidgeon-sv/

fakeroot dpkg-deb --build /tmp/pidgeonsv_deb pidgeon-sv.deb

rm -rf /tmp/pidgeonsv_deb
