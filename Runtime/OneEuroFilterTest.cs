namespace Nomad.Demo
{
	using UnityEngine;

	public class OneEuroFilterTest : MonoBehaviour
	{
		[SerializeField] private Transform _inputTransform;
		[SerializeField] private Transform _outputTransform;

		[SerializeField] private bool _filtersEnabled = true;
		[SerializeField]
		private OneEuroFilterParameters _positionParameters = new OneEuroFilterParameters()
		{
			Frequency = 120f,
			MinCutoff = 1f,
			Beta = 5f,
			DerivativeCutoff = 1f,
		};
		[SerializeField]
		private OneEuroFilterParameters _rotationParameters = new OneEuroFilterParameters()
		{
			Frequency = 120f,
			MinCutoff = 1f,
			Beta = 1f,
			DerivativeCutoff = 1f,
		};

		private OneEuroFilterVector3 _posFilter;
		private OneEuroFilterQuaternion _rotFilter;

		protected void Awake()
		{
			_posFilter = new OneEuroFilterVector3(_positionParameters);
			_rotFilter = new OneEuroFilterQuaternion(_rotationParameters);
		}

		protected void Update()
		{
			var rawPos = _inputTransform.position;
			var rawRot = _inputTransform.rotation;

			if (_filtersEnabled)
			{
				_posFilter.UpdateParameters(_positionParameters);
				_rotFilter.UpdateParameters(_rotationParameters);
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
