using UnityEngine;

namespace HollowKeep.Enemies
{
    public class TestMinionEnemy : Enemy
    {
        protected override int DefaultMaxHealth => 60;
        protected override float DefaultMoveSpeed => 3.5f;
        protected override int DefaultGoldReward => 10;
        protected override int DefaultDamageToCastle => 1;
    }
}
