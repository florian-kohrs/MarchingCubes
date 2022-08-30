using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHasValue<T>
{

    T Value { get; }

}
