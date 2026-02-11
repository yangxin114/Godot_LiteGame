using Godot;
using System;
using StateMachine;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 死亡状态（Dead）。进入后停止行为，可在此处播放死亡动画并释放节点。
    /// </summary>
    public class DeadState : State
    {
        public DeadState(Node owner) : base(owner) { }

        public override void Enter()
        {
            // 死亡时的处理，例如播放死亡动画、禁用碰撞等。
            // 此处示例：禁用父节点的处理（如果存在）
            if (owner is Node2D n)
            {
                n.SetProcess(false);
                n.Visible = false;
            }
        }
    }
}
