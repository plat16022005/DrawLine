using UnityEditor;
using UnityEngine;

public static class WireStoryTypingSfxEditor
{
    [MenuItem("Tools/Story/Wire Typing SFX")]
    public static void Execute()
    {
        var storyGo = GameObject.Find("TellStory");
        if (storyGo == null)
        {
            Debug.LogError("TellStory GameObject not found in the active scene.");
            return;
        }

        var controller = storyGo.GetComponent<StoryController>();
        if (controller == null)
        {
            Debug.LogError("StoryController not found on TellStory.");
            return;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/typing.mp3");
        if (clip == null)
        {
            Debug.LogError("Assets/Sound/typing.mp3 not found or not imported as AudioClip.");
            return;
        }

        // Ensure AudioSource.
        var src = storyGo.GetComponent<AudioSource>();
        if (src == null)
            src = Undo.AddComponent<AudioSource>(storyGo);
        src.playOnAwake = false;
        src.loop = false;

        controller.sfxSource = src;
        controller.typingClip = clip;
        controller.typingVolume = 1f;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(src);

        Debug.Log("Wired typing SFX for StoryController on TellStory.");
    }
}
