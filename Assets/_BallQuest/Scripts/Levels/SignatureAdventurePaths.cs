using System;
using System.Collections.Generic;
using UnityEngine;

namespace BallQuest
{
    public sealed class SignatureRoutePlan
    {
        public readonly string name;
        public readonly Vector3[] points;
        public readonly float speed;
        public SignatureRoutePlan(string name, float speed, params Vector3[] points) { this.name = name; this.points = points; this.speed = speed; }
    }

    /// <summary>Curated route descriptions for authoring and input-driven route verification. They never move a player.</summary>
    public static class SignatureAdventurePaths
    {
        public static Vector3 P(float x, float y, float z) => new Vector3(x, y, z);
        public static SignatureRoutePlan[] Variants(LevelDefinition level)
        {
            switch (level.signatureMechanic)
            {
                case "SkyMaze":
                    var maze = new List<SignatureRoutePlan>();
                    for (int lane = -1; lane <= 1; lane++) for (int exit = -1; exit <= 1; exit += 2)
                    {
                        float x = lane * 20;
                        var list = new List<Vector3> { P(0,24,0),P(0,24,13),P(0,24,22),P(x,24,22),P(x,24,33) };
                        if (lane > 0) { list.Add(P(26,24,43)); list.Add(P(26,24,57)); }
                        else list.Add(P(x,24,49));
                        list.Add(P(x,24,74)); list.Add(P(x,24,82)); list.Add(P(0,24,82)); list.Add(P(0,24,92)); list.Add(P(exit*14,24,102)); list.Add(P(exit*14,24,114));
                        maze.Add(new SignatureRoutePlan((lane < 0 ? "Risky" : lane > 0 ? "Safe" : "Mechanical") + (exit < 0 ? " west exit" : " east exit"), lane < 0 ? 4 : 5, list.ToArray()));
                    }
                    maze.Add(new SignatureRoutePlan("Hidden reward and deliberate reversal",4,P(0,24,0),P(0,24,13),P(0,24,22),P(-20,24,22),P(-20,24,46),P(-32,24,46),P(-32,24,31),P(-32,24,46),P(-20,24,46),P(-20,24,82),P(0,24,82),P(0,24,92),P(-14,24,102),P(-14,24,114)));
                    maze.Add(new SignatureRoutePlan("Dead-end exploration and return",4,P(0,24,0),P(0,24,13),P(0,24,22),P(20,24,22),P(20,24,33),P(26,24,43),P(26,24,55),P(42,24,55),P(26,24,55),P(26,24,57),P(20,24,74),P(20,24,82),P(0,24,82),P(0,24,92),P(14,24,102),P(14,24,114)));
                    return maze.ToArray();
                case "ClockworkChaos":
                    return new[] {
                        new SignatureRoutePlan("Drawbridge, gear disc and wheel",5,P(0,24,0),P(0,24,10),P(0,24,13),P(0,24,24),P(0,24,34),P(0,24,44),P(0,24,54),P(0,24,59),P(0,24,75),P(0,24,89),P(0,24,110)),
                        new SignatureRoutePlan("Outside wheel bypass",4.5f,P(0,24,0),P(0,24,10),P(0,24,13),P(0,24,24),P(0,24,34),P(0,24,44),P(18,24,44),P(18,24,86),P(0,24,86),P(0,24,110)) };
                case "GravityLab":
                    var gravity = new List<Vector3> { P(0,18,0),P(0,18,10),P(0,18,20),P(0,18,24) };
                    for(int i=1;i<=36;i++) { float a=(-90+i*5)*Mathf.Deg2Rad;gravity.Add(P(0,30+Mathf.Sin(a)*12,24+Mathf.Cos(a)*12)); }
                    gravity.AddRange(new[]{P(0,42,20),P(0,42,12),P(0,42,0),P(0,42,-8)});
                    return new[]{new SignatureRoutePlan("Floor to wall to ceiling",4.5f,gravity.ToArray())};
                case "HimalayanPass":
                    return new[] {
                        new SignatureRoutePlan("Icy mountain switchback",4.5f,P(0,30,0),P(0,30,12),P(0,30,17),P(12,32,31),P(12,32,46),P(4,32,58),P(-8,32,64),P(-14,32,74),P(-14,32,88),P(0,30,102),P(0,30,116)),
                        new SignatureRoutePlan("Long cliff path",4.5f,P(0,30,0),P(0,30,12),P(0,30,17),P(-22,30,29),P(-22,30,56),P(-22,32,64),P(-12,32,70),P(-14,32,76),P(-14,32,88),P(0,30,102),P(0,30,116)) };
                case "VolcanoIslands":
                    return new[] {
                        new SignatureRoutePlan("Ramp jump and fragile bridge",7,P(0,24,0),P(0,24,12),P(0,24,16),P(0,26,25),P(0,24,32),P(0,24,41),P(0,24,56),P(0,24,66),P(0,24,80),P(0,24,94),P(0,24,110)),
                        new SignatureRoutePlan("Moving stone detour",4.5f,P(0,24,0),P(0,24,12),P(0,24,16),P(18,24,26),P(18,24,37),P(18,24,51),P(18,24,60),P(0,24,66),P(0,24,80),P(0,24,110)) };
                default: return Array.Empty<SignatureRoutePlan>();
            }
        }
    }
}
