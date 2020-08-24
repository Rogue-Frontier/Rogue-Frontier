using SadConsole.Input;
using System;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using Console = SadConsole.Console;
using System.Linq;
using System.Collections.Generic;

static class UI {
    public static char indexToLetter(int index) {
        if (index < 26) {
            return (char)('a' + index);
        } else {
            return '\0';
        }
    }
    public static int letterToIndex(char ch) {
        ch = char.ToLower(ch);
        if (ch >= 'a' && ch <= 'z') {
            return (ch - 'a');
        } else {
            return -1;
        }
    }


    public static char indexToKey(int index) {
        //0 is the last key; 1 is the first
        if (index < 10) {
            return (char)('0' + (index + 1) % 10);
        } else {
            index -= 10;
            if (index < 26) {
                return (char)('a' + index);
            } else {
                return '\0';
            }
        }
    }
    public static int keyToIndex(char ch) {
        //0 is the last key; 1 is the first
        if (ch >= '0' && ch <= '9') {
            return (ch - '0' + 9) % 10;
        } else {
            ch = char.ToLower(ch);
            if (ch >= 'a' && ch <= 'z') {
                return (ch - 'a') + 10;
            } else {
                return -1;
            }
        }
    }
}