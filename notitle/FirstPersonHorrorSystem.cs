public class FirstPersonHorrorSystem : MonoBehaviour
{
    // Use 'new' keyword to explicitly hide the inherited 'Component.rigidbody' member
    public new Rigidbody rigidbody;

    void Start()
    {
        // Initialize the Rigidbody component
        rigidbody = GetComponent<Rigidbody>();
    }
}
