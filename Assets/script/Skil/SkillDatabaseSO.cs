using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "SkillTree/Skill Database")]
public class SkillDatabaseSO : ScriptableObject
{
    public List<SkillData> allSkills = new List<SkillData>();

    private Dictionary<string, SkillData> skillDict = new Dictionary<string, SkillData>();

    public void Initialize()
    {
        skillDict.Clear();
        foreach (var skill in allSkills)
        {
            if (skill != null && !skillDict.ContainsKey(skill.skillID))
            {
                skillDict.Add(skill.skillID, skill);
            }
        }
    }

    public SkillData GetSkillByID(string id)
    {
        if (skillDict.Count == 0) Initialize();

        skillDict.TryGetValue(id, out var skill);
        return skill;
    }
}
