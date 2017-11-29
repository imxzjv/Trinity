using System;
using System.Management.Instrumentation;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Trinity.Components.Combat;
using Trinity.Components.Combat.Resources;
using Trinity.Framework;
using Trinity.Framework.Objects;
using Zeta.Common;
using Zeta.Game;

namespace Trinity.Components.Swarm
{
    [DataContract]
    public class SwarmPlayer : IPartyMember
    {
        public void Update()
        {
            var actor = Core.Actors.Me;
            ActorSnoId = actor.ActorSnoId;
            AcdId = actor.AcdId;
            Name = actor.Name;
            Type = actor.Type;
            Position = actor.Position;
            WorldDynamicId = actor.WorldDynamicId;
            HeroId = actor.HeroId;
            ActorClass = actor.ActorClass;
            HitpointsMaxTotal = actor.HitPointsMax;
            IsInCombat = TrinityCombat.IsInCombat;
            Target = TrinityCombat.Targeting.CurrentTarget;
            MemberId = actor.MemberId;
            ActorClass = actor.ActorClass;
            Role = PartyRole.None;
            Target = Target;
            LeashDistance = 0f;
        }

        [DataMember]
        public int ActorSnoId { get; set; }
        [DataMember]
        public int AcdId { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public TrinityObjectType Type { get; set; }
        [DataMember]
        public Vector3 Position { get; set; }
        [DataMember]
        public int WorldDynamicId { get; set; }
        [DataMember]
        public int MemberId { get; set; }
        [DataMember]
        public ActorClass ActorClass { get; set; }
        [DataMember]
        public PartyRole Role { get; set; }
        [DataMember]
        public PartyObjective Objective { get; set; }
        public ITargetable Target { get; set; }
        [DataMember]
        public float LeashDistance { get; set; }
        public bool IsInCombat { get; set; }
        [DataMember]
        public int HeroId { get; set; }
        [DataMember]
        public double HitpointsMaxTotal { get; set; }


        public float Distance => Core.Player.Position.Distance(Position);
        public bool IsLeader => Role == PartyRole.Leader;
        public bool IsFollower => Role != PartyRole.Leader;
        public bool IsMe => HeroId == Core.Player.HeroId;
        public override string ToString() => $"{GetType().Name}: {Name} ({ActorClass}) Dist: {Distance}";
    }
}
