using System;

namespace TryEverything.Data
{
    [Flags]
    enum DifficultyLevels
    {
        Easy = 1,
        Normal = 2,
        Hard = 4,
        Expert = 8,
        ExpertPlus = 16
    }
}
