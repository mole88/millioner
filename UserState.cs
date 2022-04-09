namespace SovelevCore
{
    internal class UserState
    {
        public Quest currentQuest { get; set; }
        public int currentWinAmount { get; set; }
        public int fiftyfHelp { get; set; }
        public int changeQuest { get; set; }
        public int missHelp { get; set; }
        public bool canMiss { get; set; } = false;
        public int state { get; set; }
        public int fireproofAmount { get; set; }
        public bool isFireproofSetting { get; set; } = false;
    }
}
