using UnityEngine;
using UnityEditor;
using NeoSurvive.Weapon;
using NeoSurvive.Map;

namespace NeoSurvive.Editor
{
    public class SceneAutoSetup : EditorWindow
    {
        [MenuItem("NeoSurvive/Setup Scene Sprites")]
        public static void Setup()
        {
            // 스프라이트 로드
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Circle.png");
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/Square.png");

            if (circleSprite == null || squareSprite == null)
            {
                Debug.LogError("기본 스프라이트를 찾을 수 없습니다. 2D Sprite 패키지가 설치되어 있는지 확인하세요.");
                return;
            }

            // 플레이어 설정
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = circleSprite;
                    sr.color = Color.cyan;
                }
            }

            // 적 설정
            GameObject enemy = GameObject.Find("Enemy");
            if (enemy != null)
            {
                var sr = enemy.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = squareSprite;
                    sr.color = Color.red;
                }
            }

            // 투사체 설정
            GameObject projectile = GameObject.Find("ProjectilePrefab");
            if (projectile != null)
            {
                var sr = projectile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = squareSprite;
                    sr.color = Color.yellow;
                    projectile.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
                }
            }

            // 타일맵 설정
            GameObject floor = GameObject.Find("FloorTilemap");
            if (floor != null)
            {
                var generator = floor.GetComponent<MapGenerator>();
                if (generator != null)
                {
                    // 타일 에셋이 없으므로 임시로 타일 생성 로직은 생략하거나 
                    // 기본 타일을 찾아서 할당해야 함
                }
            }

            Debug.Log("씬 스프라이트 설정 완료!");
        }
    }
}
