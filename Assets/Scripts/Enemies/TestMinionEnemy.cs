using UnityEngine;

namespace HollowKeep.Enemies
{
    public class TestMinionEnemy : Enemy
    {
        public override int MaxHealth => 60;
        public override float MoveSpeed => 3.5f;
        public override int GoldReward => 10;
        public override int DamageToCastle => 1;
    }
}
