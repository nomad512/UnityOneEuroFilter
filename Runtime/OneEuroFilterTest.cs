namespace Nomad.Demo
{
	using UnityEngine;

	public class OneEuroFilterTest : MonoBehaviour
	{
		[SerializeField] private Transform _inputTransform;
		[SerializeField] private Transform _outputTransform;

		[SerializeField] private bool _filtersEnabled = true;
		[SerializeField] private OneEuroFilterVector3 _posFilter;
		[SerializeField] private OneEuroFilterQuaternion _rotFilter;

		protected void Awake()
		{
			_posFilter = new OneEuroFilterVector3();
			_rotFilter = new OneEuroFilterQuaternion();
		}

		protected void Update()
		{
			var rawPos = _inputTransform.position;
			var rawRot = _inputTransform.rotation;

			if (_filtersEnabled)
			{
				_outputTransform.position = _posFilter.Filter(rawPos);
				_outputTransform.rotation = _rotFilter.Filter(rawRot);
			}
			else
			{
				_outputTransform.position = rawPos;
				_outputTransform.rotation = rawRot;
			}
		}
	}
}
