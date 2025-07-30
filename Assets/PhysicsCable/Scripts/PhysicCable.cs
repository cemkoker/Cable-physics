using System.Collections.Generic;
using UnityEngine;

namespace HPhysic
{
    public class PhysicCable : MonoBehaviour
    {
        [Header("Look")] [SerializeField, Min(1)]
        private int numberOfPoints = 3;

        [SerializeField, Min(0.01f)] private float space = 0.3f;
        [SerializeField, Min(0.01f)] private float size = 0.3f;

        [Header("Behaviour")] [SerializeField, Min(1f)]
        private float springForce = 200;

        [SerializeField, Min(1f)] private float brakeLengthMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float minBrakeTime = 1f;

        private float brakeLength;
        private float timeToBrake = 1f;

        [Header("Object to set")] [SerializeField]
        private GameObject start;

        [SerializeField] private GameObject end;
        [SerializeField] private GameObject connector0;
        [SerializeField] private GameObject point0;

        private List<Transform> points;
        private List<Transform> connectors;
        private const string CLONE_TEXT = "Part";

        // Cached components for optimization
        private Connector startConnector;
        private Connector endConnector;
        private Rigidbody endRigidbody;

        // Optimization flags
        private bool needsUpdate = true;
        private Vector3 lastStartPos, lastEndPos;

        private void Start()
        {
            InitializeComponents();
            InitializeLists();
            CalculateBrakeLength();
        }

        private void InitializeComponents()
        {
            if (start != null) startConnector = start.GetComponent<Connector>();
            if (end != null) endConnector = end.GetComponent<Connector>();
            if (end != null) endRigidbody = end.GetComponent<Rigidbody>();
        }

        private void InitializeLists()
        {
            points = new List<Transform>();
            connectors = new List<Transform>();

            if (start != null) points.Add(start.transform);
            if (point0 != null) points.Add(point0.transform);
            if (connector0 != null) connectors.Add(connector0.transform);

            // Add existing points and connectors
            for (int i = 1; i < numberOfPoints; i++)
            {
                Transform conn = GetConnector(i);
                if (conn != null) connectors.Add(conn);

                Transform point = GetPoint(i);
                if (point != null) points.Add(point);
            }

            Transform endConn = GetConnector(numberOfPoints);
            if (endConn != null) connectors.Add(endConn);

            if (end != null) points.Add(end.transform);
        }

        private void CalculateBrakeLength()
        {
            brakeLength = space * numberOfPoints * brakeLengthMultiplier + 2f;
        }

        private void Update()
        {
            if (!ValidateComponents()) return;

            // Check if positions changed significantly to avoid unnecessary updates
            bool positionsChanged = HasPositionsChanged();

            float cableLength = UpdateConnectors(positionsChanged);
            HandleCableBreaking(cableLength);

            // Update cached positions
            lastStartPos = start.transform.position;
            lastEndPos = end.transform.position;
        }

        private bool ValidateComponents()
        {
            return start != null && end != null && startConnector != null && endConnector != null;
        }

        private bool HasPositionsChanged()
        {
            const float threshold = 0.001f;
            return Vector3.Distance(start.transform.position, lastStartPos) > threshold ||
                   Vector3.Distance(end.transform.position, lastEndPos) > threshold ||
                   needsUpdate;
        }

        private float UpdateConnectors(bool forceUpdate)
        {
            float cableLength = 0f;
            bool isConnected = startConnector.IsConnected || endConnector.IsConnected;

            if (!forceUpdate && !isConnected) return cableLength;

            int numOfParts = connectors.Count;
            Transform lastPoint = points[0];

            for (int i = 0; i < numOfParts && i + 1 < points.Count; i++)
            {
                Transform nextPoint = points[i + 1];
                Transform connector = connectors[i];

                UpdateSingleConnector(lastPoint, nextPoint, connector, forceUpdate);

                if (isConnected)
                {
                    cableLength += Vector3.Distance(lastPoint.position, nextPoint.position);
                }

                lastPoint = nextPoint;
            }

            needsUpdate = false;
            return cableLength;
        }

        private void UpdateSingleConnector(Transform startPoint, Transform endPoint, Transform connector, bool forceUpdate)
        {
            Vector3 startPos = startPoint.position;
            Vector3 endPos = endPoint.position;
            Vector3 centerPos = (startPos + endPos) * 0.5f;

            if (forceUpdate || Vector3.Distance(connector.position, centerPos) > 0.001f)
            {
                connector.position = centerPos;

                if (startPos == endPos)
                {
                    connector.localScale = Vector3.zero;
                }
                else
                {
                    connector.rotation = Quaternion.LookRotation(endPos - startPos);
                    float distance = Vector3.Distance(startPos, endPos);
                    connector.localScale = new Vector3(size, size, distance * 0.5f);
                }
            }
        }

        private void HandleCableBreaking(float cableLength)
        {
            bool isConnected = startConnector.IsConnected || endConnector.IsConnected;

            if (!isConnected) return;

            if (cableLength > brakeLength)
            {
                timeToBrake -= Time.deltaTime;
                if (timeToBrake <= 0f)
                {
                    startConnector.Disconnect();
                    endConnector.Disconnect();
                    timeToBrake = minBrakeTime;
                }
            }
            else
            {
                timeToBrake = minBrakeTime;
            }
        }

        [ContextMenu("Update Points")]
        public void UpdatePoints()
        {
            if (!ValidateRequiredObjects()) return;

            ClearOldPoints();
            GenerateNewPoints();
            needsUpdate = true;
        }

        private bool ValidateRequiredObjects()
        {
            if (start == null || end == null || point0 == null || connector0 == null)
            {
                Debug.LogWarning("Can't update because one of objects to set is null!");
                return false;
            }

            return true;
        }

        private void ClearOldPoints()
        {
            // Collect all children to destroy first, then destroy them
            List<GameObject> toDestroy = new List<GameObject>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith(CLONE_TEXT))
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject obj in toDestroy)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }

        private void GenerateNewPoints()
        {
            Vector3 currentPos = start.transform.position;
            Rigidbody lastBody = start.GetComponent<Rigidbody>();
            Vector3 direction = transform.forward * space;

            for (int i = 0; i < numberOfPoints; i++)
            {
                GameObject currentConnector = i == 0 ? connector0 : CreateNewConnector(i);
                GameObject currentPoint = i == 0 ? point0 : CreateNewPoint(i);

                Vector3 newPos = currentPos + direction;
                SetupPoint(currentPoint, newPos, lastBody);
                SetupConnector(currentConnector, currentPos, newPos);

                lastBody = currentPoint.GetComponent<Rigidbody>();
                currentPos = newPos;
            }

            // Setup end connection
            Vector3 endPos = currentPos + direction;
            end.transform.position = endPos;

            SpringJoint endSpring = lastBody.gameObject.GetComponent<SpringJoint>();
            if (endSpring == null) endSpring = lastBody.gameObject.AddComponent<SpringJoint>();
            SetupSpring(endSpring, endRigidbody);

            GameObject endConnector = CreateNewConnector(numberOfPoints);
            SetupConnector(endConnector, currentPos, endPos);
        }

        private void SetupPoint(GameObject point, Vector3 position, Rigidbody connectedBody)
        {
            point.transform.position = position;
            point.transform.localScale = Vector3.one * size;
            point.transform.rotation = transform.rotation;

            SpringJoint spring = point.GetComponent<SpringJoint>();
            SetupSpring(spring, connectedBody);
        }

        private void SetupConnector(GameObject connector, Vector3 startPos, Vector3 endPos)
        {
            connector.transform.position = (startPos + endPos) * 0.5f;
            connector.transform.rotation = Quaternion.LookRotation(endPos - startPos);

            float distance = Vector3.Distance(startPos, endPos);
            connector.transform.localScale = new Vector3(size, size, distance * 0.5f);
        }

        [ContextMenu("Add Point")]
        public void AddPoint()
        {
            Transform lastPrevPoint = GetPoint(numberOfPoints - 1);
            if (lastPrevPoint == null)
            {
                Debug.LogWarning($"Don't found point number {numberOfPoints - 1}");
                return;
            }

            // Remove existing connection to end
            RemoveSpringConnection(lastPrevPoint, endRigidbody);

            GameObject newPoint = CreateNewPoint(numberOfPoints);
            GameObject newConnector = CreateNewConnector(numberOfPoints + 1);

            // Setup new point
            newPoint.transform.position = end.transform.position;
            newPoint.transform.rotation = end.transform.rotation;
            newPoint.transform.localScale = Vector3.one * size;

            Rigidbody newPointRB = newPoint.GetComponent<Rigidbody>();
            Rigidbody lastPrevRB = lastPrevPoint.GetComponent<Rigidbody>();

            SetupSpring(newPoint.GetComponent<SpringJoint>(), lastPrevRB);
            SpringJoint endSpring = newPoint.AddComponent<SpringJoint>();
            SetupSpring(endSpring, endRigidbody);

            // Move end point
            end.transform.position += end.transform.forward * space;

            // Setup new connector
            SetupConnector(newConnector, newPoint.transform.position, end.transform.position);

            numberOfPoints++;
            CalculateBrakeLength();
            needsUpdate = true;
        }

        [ContextMenu("Remove Point")]
        public void RemovePoint()
        {
            if (numberOfPoints < 2)
            {
                Debug.LogWarning("Cable can't be shorter than 1");
                return;
            }

            Transform lastPoint = GetPoint(numberOfPoints - 1);
            Transform lastConnector = GetConnector(numberOfPoints);
            Transform secondLastPoint = GetPoint(numberOfPoints - 2);

            if (lastPoint == null || lastConnector == null || secondLastPoint == null)
            {
                Debug.LogWarning("Failed to find required components for point removal");
                return;
            }

            // Create new connection from second-to-last point to end
            SpringJoint newEndSpring = secondLastPoint.gameObject.AddComponent<SpringJoint>();
            SetupSpring(newEndSpring, endRigidbody);

            // Move end to last point position
            end.transform.position = lastPoint.position;
            end.transform.rotation = lastPoint.rotation;

            // Clean up
            if (Application.isPlaying)
            {
                Destroy(lastPoint.gameObject);
                Destroy(lastConnector.gameObject);
            }
            else
            {
                DestroyImmediate(lastPoint.gameObject);
                DestroyImmediate(lastConnector.gameObject);
            }

            numberOfPoints--;
            CalculateBrakeLength();
            needsUpdate = true;
        }

        private void RemoveSpringConnection(Transform point, Rigidbody targetBody)
        {
            SpringJoint[] springs = point.GetComponents<SpringJoint>();
            foreach (SpringJoint spring in springs)
            {
                if (spring.connectedBody == targetBody)
                {
                    if (Application.isPlaying)
                        Destroy(spring);
                    else
                        DestroyImmediate(spring);
                    break;
                }
            }
        }

        public void SetupSpring(SpringJoint spring, Rigidbody connectedBody)
        {
            if (spring == null || connectedBody == null) return;

            spring.connectedBody = connectedBody;
            spring.spring = springForce;
            spring.damper = 0.2f;
            spring.autoConfigureConnectedAnchor = false;
            spring.anchor = Vector3.zero;
            spring.connectedAnchor = Vector3.zero;
            spring.minDistance = space;
            spring.maxDistance = space;
        }

        private GameObject CreateNewPoint(int index)
        {
            GameObject temp = Instantiate(point0, transform);
            temp.name = $"{CLONE_TEXT}_{index}_Point";
            return temp;
        }

        private GameObject CreateNewConnector(int index)
        {
            GameObject temp = Instantiate(connector0, transform);
            temp.name = $"{CLONE_TEXT}_{index}_Conn";
            return temp;
        }

        // Utility methods
        private Transform GetConnector(int index) =>
            index > 0 ? transform.Find($"{CLONE_TEXT}_{index}_Conn") : connector0?.transform;

        private Transform GetPoint(int index) =>
            index > 0 ? transform.Find($"{CLONE_TEXT}_{index}_Point") : point0?.transform;

        // Public properties
        public Connector StartConnector => startConnector;
        public Connector EndConnector => endConnector;
        public IReadOnlyList<Transform> Points => points;
        public int NumberOfPoints => numberOfPoints;
    }
}