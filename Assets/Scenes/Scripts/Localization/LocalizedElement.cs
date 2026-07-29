using UnityEngine;

public enum ContentLanguage { English, Filipino }

// Attach this to each trivia object (both the original and your duplicated,
// translated copy) so RandomLoadingTrivia knows which language it belongs to.
public class LocalizedElement : MonoBehaviour
{
    [SerializeField] private ContentLanguage language = ContentLanguage.English;
    public ContentLanguage Language => language;
}