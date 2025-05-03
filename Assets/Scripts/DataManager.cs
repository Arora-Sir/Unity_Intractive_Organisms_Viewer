using UnityEngine;

/// <summary>
/// Manages data loading and selection for different microorganism simulations.
/// Implemented as a singleton to provide global access to data functionality across scenes.
/// </summary>
public class DataManager : MonoBehaviour
{
    // Singleton instance accessible from anywhere
    public static DataManager Instance { get; private set; }

    /// <summary>
    /// Enum representing the different microorganism data sets available in the application.
    /// </summary>
    public enum DataSet
    {
        Amoeba = 0,
        Euglena = 1,
        Paramecium = 2
    }

    /// <summary>
    /// The currently selected microorganism data set.
    /// </summary>
    [Tooltip("The currently active microorganism data set")]
    public DataSet currentDataSet = DataSet.Amoeba;

    /// <summary>
    /// Sets up the singleton pattern and ensures this object persists between scenes.
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            // This is the first instance - make it the singleton
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Another instance already exists - destroy this duplicate
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Returns the appropriate JSON filename for the currently selected data set.
    /// </summary>
    /// <returns>The filename for the current data set's JSON file</returns>
    public string GetCurrentDataFileName()
    {
        switch (currentDataSet)
        {
            case DataSet.Amoeba:
                return "amoeba.json";
            case DataSet.Euglena:
                return "euglena.json";
            case DataSet.Paramecium:
                return "paramecium.json";
            default:
                Debug.LogWarning("Unknown data set, defaulting to amoeba.json");
                return "amoeba.json";
        }
    }

    /// <summary>
    /// Sets the current data set based on an integer index.
    /// Useful for UI elements like dropdowns that work with integer values.
    /// </summary>
    /// <param name="index">The index corresponding to the DataSet enum value</param>
    public void SetDataSetByIndex(int index)
    {
        Debug.Log($"Setting data set to index: {index}");

        // Check if the index is a valid enum value
        if (System.Enum.IsDefined(typeof(DataSet), index))
        {
            currentDataSet = (DataSet)index;
            Debug.Log($"Current data set is now: {currentDataSet}");
        }
        else
        {
            Debug.LogError($"Invalid data set index: {index}. Valid range is 0-{System.Enum.GetValues(typeof(DataSet)).Length - 1}");
        }
    }

    /// <summary>
    /// Sets the current data set directly using the DataSet enum.
    /// Provides a type-safe way to change the active data set.
    /// </summary>
    /// <param name="dataSet">The data set to switch to</param>
    public void SetDataSet(DataSet dataSet)
    {
        currentDataSet = dataSet;
        Debug.Log($"Current data set is now: {currentDataSet}");
    }
}
