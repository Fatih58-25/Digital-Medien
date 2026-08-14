using TMPro;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(TMP_Text))]
public class CurvedText: MonoBehaviour
{
    public float centerScale = 0.8f;
    public float edgeScale = 1.2f;

    private TMP_Text text;

    void Awake() 
    { 
        text = GetComponent<TMP_Text>(); 
    }

    void LateUpdate()
    {
        ApplyEffect();
    }

    void ApplyEffect()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        text.ForceMeshUpdate();

        TMP_TextInfo textInfo = text.textInfo;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

      
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo c = textInfo.characterInfo[i];
            if (!c.isVisible) continue;

            minX = Mathf.Min(minX, c.bottomLeft.x);
            maxX = Mathf.Max(maxX, c.topRight.x);
        }

        float centerX = (minX + maxX) * 0.5f;
        float halfWidth = Mathf.Max(0.0001f, (maxX - minX) * 0.5f);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo c = textInfo.characterInfo[i];
            if (!c.isVisible) continue;

            int matIndex = c.materialReferenceIndex;
            int vertIndex = c.vertexIndex;

            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            Vector3 bl = verts[vertIndex + 0];
            Vector3 tl = verts[vertIndex + 1];
            Vector3 tr = verts[vertIndex + 2];
            Vector3 br = verts[vertIndex + 3];

            Vector3 charCenter = (bl + tr) * 0.5f;

            float t = Mathf.Abs(charCenter.x - centerX) / halfWidth;
            float scale = Mathf.Lerp(centerScale, edgeScale, t);

            Vector3 pivot = charCenter;

            verts[vertIndex + 0] = pivot + (bl - pivot) * scale;
            verts[vertIndex + 1] = pivot + (tl - pivot) * scale;
            verts[vertIndex + 2] = pivot + (tr - pivot) * scale;
            verts[vertIndex + 3] = pivot + (br - pivot) * scale;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}