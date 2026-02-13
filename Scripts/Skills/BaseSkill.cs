using Godot;
using Skills;
using System;

namespace Skills
{
    public partial class BaseSkill : Node2D
    {
        public ISkillOwner SkillOwner { get; set; }

        public override void _Ready()
        {
            
        }

    }

}