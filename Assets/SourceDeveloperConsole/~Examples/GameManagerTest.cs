using Farbod.DeveloperConsole;
using System;
using UnityEngine;

namespace Farbod.DeveloperConsole.Examples
{
    /// <summary>
    /// A singleton GameManager-like class
    /// </summary>
    public class GameManagerTest : MonoBehaviour
    {
        public static GameManagerTest instance;

        [SerializeField] public int difficulty = 0;
        [SerializeField] GameObject _cubeprefab;

        /// <summary>
        /// This command sets a value on our class's singleton instance. but first we check to see if the instance is set or not.
        /// </summary>
        [ConsoleVariable("difficulty", "(example command) The difficulty index of the game. (0 to 3)")]
        public static int Difficulty
        {
            get
            {
                if (instance == null)
                    throw new NoGameManagerException("You need to start a game to access a variable from the GameManager");

                return instance.difficulty;
            }
            set
            {
                if (instance == null)
                    throw new NoGameManagerException("You need to start a game to access a variable from the GameManager");

                if (value < 0 || value > 3)
                    throw new ArgumentOutOfRangeException(nameof(value));

                instance.difficulty = value;
            }
        }

        [ConsoleMethod("createcube", "(example command) Creates a cube at the world origin")]
        public static void CreateCube(float scale = 1f)
        {
            if (instance == null)
                throw new NoGameManagerException("You need to start a game to access a variable from the GameManager");

            instance.InstantiateCubePrefab(scale);
        }

        public void InstantiateCubePrefab(float scale = 1f)
        {
            var cube = Instantiate(_cubeprefab);
            cube.transform.parent = transform;
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = new(scale, scale, scale);
        }
        private void Awake()
        {
            // Prevent multiple instances
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
        }
    }

    public class NoGameManagerException : Exception
    {
        public NoGameManagerException() { }
        public NoGameManagerException(string message) : base(message) { }
    }
}

