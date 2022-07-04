/*
MIT License

Copyright (c) 2022 Joseph Cramsey

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

public enum PingChannelID {
    Player_WasSeen = 0,
    Player_WasAsked = 1,
    Target_WasSeen = 2,
    Target_WasAsked = 3,
    Predator_WasSeen = 4,
    Predator_WasAsked = 5,
    Freighter_WasSeen = 6,
    Freighter_WasAsked = 7,
    Count = 8
}

public enum PingStrength {
    Strong = 3,
    Good = 2,
    Weak = 1,
    Silent = 0
}

public enum PingDirection {
    Surrounding = 0,
    North = 1,
    East = 2,
    South = 3,
    West = 4,
    Count = 5
}

public enum ShipSpeed {
    Stealth = -2,
    Halted = -1,
    Cruise = 0,
    AheadFull = 1
}

public enum TideMode {
    Standard,
    Unseen,
    Crowding
}
