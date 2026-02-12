using Godot;

namespace Skills
{
    public partial interface ISkillOwner
    {
        public Node2D GetNode2DOwner();
    }
}