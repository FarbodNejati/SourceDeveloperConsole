using Farbod.DeveloperConsole;
using System;
using UnityEngine;

namespace Farbod.DeveloperConsole.Examples
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
        VR
    }
    /// <summary>
    /// A singleton GameManager-like class
    /// </summary>
    public class GameManagerTest : MonoBehaviour
    {
        public static GameManagerTest instance;

        [SerializeField] public int difficulty = 0;
        [SerializeField] GameObject _cubeprefab;

        /// <summary>
        /// The simplest way to set up a console variable
        /// </summary>
        [ConsoleVariable("eg_cameramode", "(example command) Set the Camera mode enum setting")]
        private static CameraMode _camMode { get; set; }

        private static int _staticNumber = 6;

        /// <summary>
        /// A console variable with a proper getter and setter
        /// This command sets a value on our class's singleton instance. but first we check to see if the instance is set or not.
        /// </summary>
        [ConsoleVariable("eg_difficulty", "(example command) The difficulty index of the game. (0 to 3)")]
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

        /// <summary>
        /// The simplest way to set up a console command.
        /// </summary>
        [ConsoleMethod("eg_createcube", "(example command) Creates a cube at the world origin")]
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

        /// <summary>
        /// The simplest way to set up a console command.
        /// </summary>
        [ConsoleMethod("eg_setsettings", "(example command) Just testing method parameters")]
        public static void SetSettings(int test_number ,CameraMode camMode)
        {
            _staticNumber = test_number;
            _camMode = camMode;
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

