namespace Manatite.Core.SaveSystem
{
    /// <summary>Static handoff: menu code sets this immediately before loading the gameplay
    /// scene — the loaded SaveData to resume, or null for a fresh random run. RunSaveController
    /// and PlanetSpawner each read it in their own Start() (never clear it themselves — only
    /// menu entry points (MainMenu.OnNewGame/OnLoadGame, PauseMenu.OnLoadGame) ever set it).</summary>
    public static class PendingLoad
    {
        public static SaveData Data { get; set; }
    }
}
