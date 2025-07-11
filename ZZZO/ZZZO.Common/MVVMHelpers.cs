﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZZZO.Common;

public class DependentPropertiesAttribute : Attribute
{
  #region Proměnné

  #endregion

  #region Vlastnosti

  public string[] Properties
  {
    get;
  }

  #endregion

  #region Konstruktory

  public DependentPropertiesAttribute(params string[] dp)
  {
    Properties = dp;
  }

  #endregion
}

/// <summary>
/// Observable object with INotifyPropertyChanged implemented
/// </summary>
public class ObservableObject : INotifyPropertyChanged
{
  #region Implementace rozhraní

  /// <summary>
  /// Occurs when property changed.
  /// </summary>
  public event PropertyChangedEventHandler PropertyChanged;

  #endregion

  #region Metody

  /// <summary>
  /// Raises the property changed event.
  /// </summary>
  /// <param name="propertyName">Property name.</param>
  protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  /// <summary>
  /// Sets the property.
  /// </summary>
  /// <returns><c>true</c>, if property was set, <c>false</c> otherwise.</returns>
  /// <param name="backingStore">Backing store.</param>
  /// <param name="value">Value.</param>
  /// <param name="validateValue">Validates value.</param>
  /// <param name="propertyName">Property name.</param>
  /// <param name="onChanged">On changed.</param>
  /// <typeparam name="T">The 1st type parameter.</typeparam>
  protected virtual bool SetProperty<T>(
    ref T backingStore, T value,
    [CallerMemberName] string propertyName = "",
    Action onChanged = null,
    Func<T, T, bool> validateValue = null)
  {
    //if value didn't change
    if (EqualityComparer<T>.Default.Equals(backingStore, value))
    {
      return false;
    }

    //if value changed but didn't validate
    if (validateValue != null && !validateValue(backingStore, value))
    {
      return false;
    }

    backingStore = value;
    onChanged?.Invoke();
    OnPropertyChanged(propertyName);
    return true;
  }

  #endregion
}