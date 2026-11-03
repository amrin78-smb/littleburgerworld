using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Customers
{
    /// <summary>
    /// Brings animal customers into the restaurant one at a time. In Free Play they keep coming
    /// with random picture requests; in Guided Play the activity decides who comes and what they want.
    /// </summary>
    public sealed class CustomerManager : MonoBehaviour
    {
        public static CustomerManager Instance { get; private set; }

        [SerializeField] private Transform m_SpawnPoint;
        [SerializeField] private Transform[] m_EnterPath;
        [SerializeField] private Transform[] m_ExitPath;
        [SerializeField] private Transform m_FaceTowards;
        [SerializeField] private Transform m_DoorHinge;
        [SerializeField] private float m_FreePlayDelay = 2f;

        private static readonly string[][] s_FreePlayRequests =
        {
            new[] { "cheeseburger" },
            new[] { "burger" },
            new[] { "fries" },
            new[] { "milkshake" },
            new[] { "burger", "fries" },
            new[] { "cheeseburger", "milkshake" },
            new[] { "burger", "fries", "milkshake" }
        };

        private AnimalCustomerController m_Current;
        private float m_Timer;
        private int m_LastCharacter = -1;
        private Quaternion m_DoorRest;

        public AnimalCustomerController Current => m_Current;
        /// <summary>Free Play: keep customers coming.</summary>
        public bool AutoSpawn { get; set; }

        private void Awake()
        {
            Instance = this;
            if (m_DoorHinge != null) m_DoorRest = m_DoorHinge.localRotation;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!AutoSpawn || m_Current != null) { m_Timer = 0f; return; }
            m_Timer += Time.deltaTime;
            if (m_Timer >= m_FreePlayDelay)
            {
                m_Timer = 0f;
                SpawnRandom();
            }
        }

        public AnimalCustomerController SpawnRandom()
        {
            CharacterData c = PickCharacter();
            string[] req = s_FreePlayRequests[Random.Range(0, s_FreePlayRequests.Length)];
            return Spawn(c, req);
        }

        private CharacterData PickCharacter()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return null;
            var available = new List<CharacterData>();
            foreach (CharacterData c in gm.Config.characters)
            {
                if (c != null && c.prefab != null && gm.Rewards.IsCharacterUnlocked(c)) available.Add(c);
            }
            if (available.Count == 0) return null;
            int idx = Random.Range(0, available.Count);
            if (available.Count > 1 && idx == m_LastCharacter) idx = (idx + 1) % available.Count;
            m_LastCharacter = idx;
            return available[idx];
        }

        /// <summary>Spawns a customer at the door and walks them to the counter.</summary>
        public AnimalCustomerController Spawn(CharacterData data, IEnumerable<string> request, bool instant = false)
        {
            if (data == null || data.prefab == null) return null;
            if (m_Current != null) DismissCurrent();

            Vector3 start = m_SpawnPoint != null ? m_SpawnPoint.position : transform.position;
            GameObject go = Instantiate(data.prefab, start, Quaternion.identity, transform);
            go.name = "Customer_" + data.id;
            AnimalCustomerController c = go.GetComponent<AnimalCustomerController>();
            if (c == null)
            {
                Debug.LogError("[CustomerManager] Character prefab has no AnimalCustomerController: " + data.id);
                Destroy(go);
                return null;
            }

            var enter = new List<Vector3>();
            foreach (Transform t in m_EnterPath) if (t != null) enter.Add(t.position);
            var exit = new List<Vector3>();
            foreach (Transform t in m_ExitPath) if (t != null) exit.Add(t.position);
            Vector3 serve = enter.Count > 0 ? enter[enter.Count - 1] : start;
            Vector3 face = m_FaceTowards != null ? m_FaceTowards.position - serve : new Vector3(-1f, 0f, -1f);
            face.y = 0f;

            c.Setup(data, enter, exit, face);
            c.SetRequest(request);
            c.Left += OnCustomerLeft;
            m_Current = c;
            if (instant) c.ArriveInstantly();
            else
            {
                OpenDoor();
                c.Enter();
            }
            return c;
        }

        /// <summary>Guided Play: make sure the named character is at the counter with this request.</summary>
        public AnimalCustomerController CallCustomer(string characterId, IEnumerable<string> request)
        {
            GameManager gm = GameManager.Instance;
            CharacterData data = gm != null ? gm.Config.FindCharacter(characterId) : null;
            if (data == null && gm != null && gm.Config.characters.Count > 0) data = gm.Config.characters[0];
            if (m_Current != null && m_Current.Data == data &&
                (m_Current.State == CustomerState.Waiting || m_Current.State == CustomerState.Entering))
            {
                m_Current.SetRequest(request);
                return m_Current;
            }
            return Spawn(data, request);
        }

        public void DismissCurrent()
        {
            if (m_Current == null) return;
            AnimalCustomerController c = m_Current;
            m_Current = null;
            c.Left -= OnCustomerLeft;
            c.Left += _ => OpenDoor();
            c.Leave();
        }

        private void OnCustomerLeft(AnimalCustomerController c)
        {
            if (c == m_Current) m_Current = null;
            OpenDoor();
        }

        private void OpenDoor()
        {
            if (m_DoorHinge == null) return;
            Tween.Kill(m_DoorHinge);
            Tween.RotateLocal(m_DoorHinge, m_DoorRest * Quaternion.Euler(0f, -80f, 0f), 0.35f, Ease.OutBack, () =>
            {
                Tween.RotateLocal(m_DoorHinge, m_DoorRest, 0.6f, Ease.OutBounce, null, 1.6f);
            });
        }
    }
}
