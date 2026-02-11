// using Godot;
// using System;
// using System.Collections.Generic;
// using System.Text.Json;

// namespace Characters
// {
//     /// <summary>
//     /// PlayerViewData：用于描述玩家外观相关数据的 Resource 类。
//     /// </summary>
//     [GlobalClass]
//     public partial class UnUsedPlayerUIData : Resource
//     {
//         [Export]
//         public Texture2D PlayerTexture { get; set; }

//         [Export]
//         public Texture2D PlayerIcon { get; set; }

//         [Export]
//         public SpriteFrames PlayerAnimationFrames { get; set; }

//         public enum PlayerAnimation
//         {
//             Idle,
//             Walk,
//             Run,
//             Attack,
//             Dash,
//             Hurt,
//             Death,
//         }
        
//         /// <summary>
//         /// 获取动画枚举对应的字符串值
//         /// </summary>
//         public static string GetAnimationString(PlayerAnimation anim)
//         {
//             return anim switch
//             {
//                 PlayerAnimation.Idle => "idle",
//                 PlayerAnimation.Walk => "walk",
//                 PlayerAnimation.Run => "run",
//                 PlayerAnimation.Attack => "attack",
//                 PlayerAnimation.Dash => "dash",
//                 PlayerAnimation.Hurt => "hurt",
//                 PlayerAnimation.Death => "death",
//                 _ => "idle"
//             };
//         }
        
//         /// <summary>
//         /// 从字符串获取对应的动画枚举
//         /// </summary>
//         public static PlayerAnimation GetAnimationEnum(string animString)
//         {
//             return animString?.ToLower() switch
//             {
//                 "idle" => PlayerAnimation.Idle,
//                 "walk" => PlayerAnimation.Walk,
//                 "run" => PlayerAnimation.Run,
//                 "attack" => PlayerAnimation.Attack,
//                 "dash" => PlayerAnimation.Dash,
//                 "hurt" => PlayerAnimation.Hurt,
//                 "death" => PlayerAnimation.Death,
//                 _ => PlayerAnimation.Idle
//             };
//         }
//     }
// }