using System;
using System.Collections.Generic;
using Windows.UI;

namespace Route
{
    public class SaveData
    {
        public SaveData()
        {
        }

        public int Round { get; set; }
        public int ColorIndex { get; set; }

        public int Money { get; set; }
        public int Score { get; set; }

        public List<bool> Stations_Userdef { get; set; } 
        public List<Color> Stations_Color { get; set; } 
        public List<double> Stations_X { get; set; } 
        public List<double> Stations_Y { get; set; }
        public List<int> Stations_Direction { get; set; }

        public List<int> Links_S1 { get; set; }
        public List<int> Links_S2 { get; set; }

        public List<int> Packets_Init { get; set; }
        public List<int> Packets_From { get; set; }
        public List<int> Packets_Dest { get; set; }
        public List<Color> Packets_Color { get; set; }
        public List<int> Packets_Link { get; set; }
        public List<double> Packets_X { get; set; }
        public List<double> Packets_Y { get; set; }
    }

    public class GameData
    {
        public GameData()
        {
            
        }

        public int HiScore { get; set; }

        public bool IsMute { get; set; }

        public bool IsMouse { get; set; }
    }

}
