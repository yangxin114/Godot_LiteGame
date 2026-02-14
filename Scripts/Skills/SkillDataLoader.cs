using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能数据加载器
    /// 负责从资源配置文件加载和管理技能数据
    /// </summary>
    public partial class SkillDataLoader : Node
    {
        #region 单例模式

        private static SkillDataLoader _instance;
        public static SkillDataLoader Instance => _instance;

        #endregion

        #region 字段和属性

        /// <summary>
        /// 技能数据字典
        /// </summary>
        private Dictionary<string, SkillData> _skillDataDict = new();

        /// <summary>
        /// 技能目录路径
        /// </summary>
        private const string SKILL_DATA_PATH = "res://Data/Skills/";

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized = false;

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            if (_instance == null)
            {
                _instance = this;
                Initialize();
            }
            else
            {
                QueueFree();
            }
        }

        public override void _ExitTree()
        {
            _instance = null;
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化数据加载器
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
            {
                Logger2.Warn("SkillDataLoader: 已经初始化过了");
                return;
            }

            LoadAllSkillData();
            _isInitialized = true;
            
            Logger2.Info("SkillDataLoader: 初始化完成");
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载所有技能数据
        /// </summary>
        private void LoadAllSkillData()
        {
            try
            {
                // 检查目录是否存在
                if (!ResourceLoader.Exists(SKILL_DATA_PATH))
                {
                    Logger2.Warn($"SkillDataLoader: 技能数据目录不存在 {SKILL_DATA_PATH}");
                    return;
                }

                // 遍历目录中的所有.tres文件
                var dir = DirAccess.Open(SKILL_DATA_PATH);
                if (dir != null)
                {
                    dir.ListDirBegin();
                    var fileName = dir.GetNext();
                    
                    while (fileName != "")
                    {
                        if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
                        {
                            LoadSkillDataFromFile(fileName);
                        }
                        fileName = dir.GetNext();
                    }
                    dir.ListDirEnd();
                }

                Logger2.Info($"SkillDataLoader: 成功加载 {_skillDataDict.Count} 个技能数据");
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillDataLoader: 加载技能数据失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载技能数据
        /// </summary>
        private void LoadSkillDataFromFile(string fileName)
        {
            try
            {
                var fullPath = SKILL_DATA_PATH + fileName;
                var skillData = ResourceLoader.Load<SkillData>(fullPath);
                
                if (skillData != null && !string.IsNullOrEmpty(skillData.SkillId))
                {
                    _skillDataDict[skillData.SkillId] = skillData;
                    Logger2.Debug($"SkillDataLoader: 加载技能数据 {skillData.SkillName} ({skillData.SkillId})");
                }
                else
                {
                    Logger2.Warn($"SkillDataLoader: 技能数据文件 {fileName} 格式不正确");
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillDataLoader: 加载技能数据文件 {fileName} 失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 动态加载技能数据
        /// </summary>
        public SkillData LoadSkillData(string skillId)
        {
            // 如果已经加载过，直接返回
            if (_skillDataDict.TryGetValue(skillId, out var cachedData))
            {
                return cachedData;
            }

            // 尝试从文件加载
            var fileName = $"{skillId}.tres";
            var fullPath = SKILL_DATA_PATH + fileName;
            
            if (ResourceLoader.Exists(fullPath))
            {
                try
                {
                    var skillData = ResourceLoader.Load<SkillData>(fullPath);
                    if (skillData != null)
                    {
                        _skillDataDict[skillId] = skillData;
                        Logger2.Debug($"SkillDataLoader: 动态加载技能数据 {skillData.SkillName} ({skillId})");
                        return skillData;
                    }
                }
                catch (Exception ex)
                {
                    Logger2.Error($"SkillDataLoader: 动态加载技能数据 {skillId} 失败 - {ex.Message}");
                }
            }

            Logger2.Warn($"SkillDataLoader: 未找到技能数据 {skillId}");
            return null;
        }

        #endregion

        #region 数据查询

        /// <summary>
        /// 获取技能数据
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            if (_skillDataDict.TryGetValue(skillId, out var skillData))
            {
                return skillData;
            }

            // 如果没找到，尝试动态加载
            return LoadSkillData(skillId);
        }

        /// <summary>
        /// 获取所有技能数据
        /// </summary>
        public SkillData[] GetAllSkillData()
        {
            return new List<SkillData>(_skillDataDict.Values).ToArray();
        }

        /// <summary>
        /// 检查技能数据是否存在
        /// </summary>
        public bool HasSkillData(string skillId)
        {
            return _skillDataDict.ContainsKey(skillId) || ResourceLoader.Exists($"{SKILL_DATA_PATH}{skillId}.tres");
        }

        /// <summary>
        /// 根据条件筛选技能数据
        /// </summary>
        public SkillData[] GetSkillDataByCondition(Func<SkillData, bool> condition)
        {
            var result = new List<SkillData>();
            
            foreach (var skillData in _skillDataDict.Values)
            {
                if (condition(skillData))
                {
                    result.Add(skillData);
                }
            }
            
            return result.ToArray();
        }

        #endregion

        #region 数据管理

        /// <summary>
        /// 重新加载所有技能数据
        /// </summary>
        public void ReloadAllData()
        {
            _skillDataDict.Clear();
            LoadAllSkillData();
            Logger2.Info("SkillDataLoader: 重新加载所有技能数据");
        }

        /// <summary>
        /// 清理缓存的数据
        /// </summary>
        public void ClearCache()
        {
            _skillDataDict.Clear();
            Logger2.Info("SkillDataLoader: 清理技能数据缓存");
        }

        /// <summary>
        /// 获取加载统计信息
        /// </summary>
        public string GetLoadStatistics()
        {
            return $"已加载技能数据: {_skillDataDict.Count} 个";
        }

        #endregion
    }
}