namespace GameAndWatch.Audio
{
    /// <summary>
    /// Centralized string constants for all sound IDs used across the project.
    /// These must match exactly the soundId field set on each SoundConfig asset.
    /// </summary>
    public static class SoundIds
    {
        public static class GameAndWatch
        {
            public const string PlayerMove    = "gaw_player_move";
            public const string GlobuleMove   = "gaw_globule_move";
            public const string GlobuleSpawn  = "gaw_globule_spawn";
            public const string PlayerHit     = "gaw_player_hit";
            public const string PlayerRespawn = "gaw_player_respawn";
            public const string HeartReached  = "gaw_heart_reached";
            public const string GameOver      = "gaw_game_over";
        }

        public static class VirusSplit
        {
            public const string Split = "vs_split";
            public const string Merge = "vs_merge";
        }

        public static class ShootEmUp
        {
            public const string PlayerShoot   = "seu_player_shoot";
            public const string PlayerHit     = "seu_player_hit";
            public const string EnergyCollect = "seu_energy_collect";
            public const string LaserReady    = "seu_laser_ready";
            public const string EnemyHit      = "seu_enemy_hit";
            public const string EnemyDeath    = "seu_enemy_death";
            public const string LaserLoop     = "seu_laser_loop";
        }

        public static class Music
        {
            public const string MainMenu    = "mus_main_menu";
            public const string FlappyBird  = "mus_flappy_bird";
            public const string ShootEmUp   = "mus_shoot_em_up";
        }
    }
}
