#!/bin/bash
#  Copyright (c) 2013, Petr Bena
#  All rights reserved.

#  Redistribution and use in source and binary forms, with
#  or without modification, are permitted provided that
#  the following conditions are met:

#  1. Redistributions of source code must retain
#     the above copyright notice, this list 
#     of conditions and the following disclaimer.

#  2. Redistributions in binary form must reproduce the above copyright
#     notice, this list of conditions and the following disclaimer in
#     the documentation and/or other materials provided with the distribution.

#  3. Neither the name of Huggle nor the names of its contributors may be used
#     to endorse or promote products derived from this software without specific
#     prior written permission.

#  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS
#  OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
#  MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
#  THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
#  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
#  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
#  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
#  OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
#  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

SKIPCHECKS=0
RED=$(tput setaf 1)
WARN=$(tput setaf 3)
GREEN=$(tput setaf 2)
NORMAL=$(tput sgr0)
QT5=10
FLAGS=''
if [ x"$*" != x ];then
    echo "Options used: $*"
fi
# first we parse all arguments we got
for var in "$@"
do
    if [ "$var" = "--help" ] || [ "$var" = "-h" ];then
        echo "Install script for pidgeon-sv, parameters:"
        echo "=========================================="
        echo " --skip-checks|--disable-dependency-tracking: skip all package checks"
        echo " --build=[i686-pc-linux-gnu|x86_64-pc-linux-gnu]: change the target architecture to specified"
        echo " --no-colors: suppress colors"
        echo " --version: show version"
        echo
        echo "Copyright (c) 2014 Petr Bena. This script is licensed under the BSD license."
        exit 0
    fi
    if [ "$var" = "--no-colors" ];then
        RED=""
        WARN=""
        GREEN=""
        NORMAL=""
    fi
    if [ "$var" = "--build=i686-pc-linux-gnu" ];then
        FLAGS='-spec linux-g++-32'
        continue
    fi
    if [ "$var" = "--build=x86_64-pc-linux-gnu" ];then
        FLAGS='-spec linux-g++-64'
        continue
    fi
    if [ "$var" = "--version" ];then
        echo "Huggle configure v 1.0"
        exit 0
    fi
    if [ "$var" = "--skip-checks" ] || [ "$var" = "--disable-dependency-tracking" ];then
        SKIPCHECKS=1
        continue
    fi
done

ok()
{
    printf '%s%s%s\n' "$GREEN" "[OK]" "$NORMAL"
}

fail()
{
    printf '%s%s%s\n' "$RED" "[FAIL]" "$NORMAL"
}

text()
{
    MSG="$1"
    let COL=$(tput cols)-20-${#MSG}+${#GREEN}+${#NORMAL}
    printf '%s%*s' "$MSG" $COL
}

checkhf()
{
    text "Check for headers of $1..."
    if [ "$SKIPCHECKS" -eq 1 ];then
        echo "SKIP"
        return 0
    fi
    if [ -f "$2" ];then
        ok
        return 0
    else
        fail
        echo "Unable to find headers for $1 ($2)"
        echo "try installing dev files for $1"
        exit 1
    fi
} 

checkqt()
{
    text "Checking if $1 or $2 is present... "
    if [ "$SKIPCHECKS" -eq 1 ];then
        echo "SKIP"
        return 0
    fi
    if [ "`apt-cache policy $1 | grep -Ev '^\s*Installed: \(none\)' | grep -E '^\s*Installed: ' | wc -l`" -gt 0 ];then
        ok
        return 0
    fi

    if [ "`apt-cache policy $2 | grep -Ev '^\s*Installed: \(none\)' | grep -E '^\s*Installed: ' | wc -l`" -gt 0 ];then
        ok
        return 0
    fi

    fail
    echo "$1 neither $2 is present, use --skip-checks to ignore this"
    echo "or execute apt-get install $1 $2 as root"
    exit 1
}


checkpkg()
{
    text "Checking if $1 is present... "
    if [ "$SKIPCHECKS" == "1" ];then
        echo "SKIP"
        return 0
    fi
    if [ "`apt-cache policy $1 | grep -Ev '^\s*Installed: \(none\)' | grep -E '^\s*Installed: ' | wc -l`" -gt 0 ];then
        ok
        return 0
    fi
    fail
    echo "$1 is not present, use --skip-checks to ignore this"
    echo "or execute apt-get install $1 as root"
    exit 1
}

serviceuser=pidgeonsv
text "Checking if system user is present..."
if [ id -u $serviceuser >/dev/null 2>&1 ];then
    ok
    text "Creating pidgeon service user..."
    if [ ! useradd -r pidgeonsv -d /var/lib/pidgeonsv ];then
        fail
        exit 1
    fi
    ok
else
ok
fi

text "Creating system folders..."

ok

