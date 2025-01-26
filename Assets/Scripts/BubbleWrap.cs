using UnityEngine;

public class BubbleWrap : MonoBehaviour
{
    public new Camera camera;

    public GameController gameController;
    public Bubble bubblePrefab;
    public GameObject bubblesRoot;
    private float bubblesWidthWS = 0;
    private float bubblesHeightWS = 0;

    private Bubble[] grid = null;
    private int hCount = 0;
    private int vCount = 0;

    // TODO: Would be maybe cleaner to use an actual game state enum (Playing, Lost, etc...).
    private bool isGameOver = false;
    // Are we an actual game session?
    // Because we want to allow popping bubbles even on the menu screen, but that's not a real game session.  
    private bool isGameSession = false;
    private bool isMouseClickOnBubblesDisabled = false;
    private bool isPaused = false;
    private bool isFirstPop = true;

    private void Update()
    {
#if UNITY_EDITOR
        Debug.DrawLine(bubblesRoot.transform.position, bubblesRoot.transform.position + bubblesRoot.transform.right * bubblesWidthWS);
        Debug.DrawLine(bubblesRoot.transform.position, bubblesRoot.transform.position + bubblesRoot.transform.up * bubblesHeightWS);
#endif

        if (isGameSession && !isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            {
                SetPaused(!isPaused);
            }
        }

        if (isMouseClickOnBubblesDisabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = camera.nearClipPlane;
            Ray r = camera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(r);
            if (hits.Length > 0)
            {
                Bubble b = hits[0].collider.gameObject.GetComponent<Bubble>();
                if (b)
                {
                    PopBubble(b);
                }
            }
        }
        else if (Input.GetMouseButtonDown(1) && isGameSession)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = camera.nearClipPlane;
            Ray r = camera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(r);
            if (hits.Length > 0)
            {
                Bubble b = hits[0].collider.gameObject.GetComponent<Bubble>();
                if (b)
                {
                    b.ToggleFlag();
                }
            }
        }
    }

    public void SetPaused(bool isPaused)
    {
        if (!isGameOver)
        {
            this.isPaused = isPaused;
            gameController.NotifyPause(isPaused);
            isMouseClickOnBubblesDisabled = isPaused;
        }
    }

    /// <summary>
    /// Initialize grid with dimCount bubbles on the height, and bombRatio amount of bombs.
    /// Bigger size is harder, and more bombs is harder.
    /// A good moderate default is something like (12, 0.2)
    /// </summary>
    public void Initialize(int dimCount, float bombRatio, bool isGameSession)
    {
        this.isGameSession = isGameSession;
        isGameOver = false;
        isFirstPop = true;

        // Start off asserting mouse clicks are allowed.
        isMouseClickOnBubblesDisabled = false;

        if (dimCount == 0) return;

        Transform cameraTransform = camera.transform;
        Vector3 cameraRightWS = cameraTransform.right;
        Vector3 cameraUpWS = cameraTransform.up;
        Vector3 cameraForwardWS = cameraTransform.forward;

        // Get initial dimensions relative to camera.
        float gridDistanceWS = Mathf.Max(1f, camera.nearClipPlane);
        Vector3 bottomLeftWS = camera.ViewportToWorldPoint(new Vector3(0, 0, gridDistanceWS));
        Vector3 topRightWS = camera.ViewportToWorldPoint(new Vector3(1, 1, gridDistanceWS));
        Vector3 viewportDiagonalWS = topRightWS - bottomLeftWS;
        float widthWS = Vector3.Project(viewportDiagonalWS, cameraRightWS).magnitude;
        float heightWS = Vector3.Project(viewportDiagonalWS, cameraUpWS).magnitude;

        // Calculate specific sizes for grid.
        float diameterWS = heightWS / dimCount;
        float radiusWS = diameterWS * 0.5f;

        // SIDE NOTE: The grid is transposed
        // from what you might think:  
        //
        //     (4)   (10)
        //   2     7  
        //      3      9     ^
        //   1     6         |
        //      2      8     X
        //   0     5           Y--->
        //
        // This happened because I first math'd it 
        // thinking about the row _width_, then later
        // realized I should done it the other way
        // where I see how many bubbles fit in the
        // vertical direction first. This is because
        // the camera's FOV is stable on the vertical
        // dimension, but not the horizontal (resizing
        // the window changes horizontal FOV).  
        //
        // So I ended up swapping the dimensions, transposing
        // the grid, this should be fine and not break
        // the existing game logic I hope...

        // Figure out how many full rows we can fit.
        // (excluding the interleaved short rows)
        vCount = 0;
        {
            float y = radiusWS;
            while ((y + diameterWS) < widthWS)
            {
                ++vCount;
                y += diameterWS;
            }
        }
        // Add short rows
        vCount += (vCount - 1);

        hCount = dimCount;

        InstantiateGridObjects();

        // Arrange bubble positions and sizes.
        float packedRadiusWS = radiusWS;
        packedRadiusWS *= packedRadiusWS;
        packedRadiusWS *= 2;
        packedRadiusWS = Mathf.Sqrt(packedRadiusWS);
        packedRadiusWS *= 0.5f;
        float packedDiameterWS = packedRadiusWS * 2.0f;
        bubblesWidthWS = 0;
        bubblesHeightWS = 0;
        {
            float x = packedRadiusWS;
            float y = packedRadiusWS;
            for (int v = 0; v < vCount; ++v)
            {
                x = ((v & 1) == 0) ? packedRadiusWS : (packedRadiusWS + radiusWS);
                for (int h = 0; h < hCount; ++h)
                {
                    if (h == hCount - 1 && ((v & 1) == 1))
                    {
                        continue;
                    }

                    Bubble b = grid[v * hCount + h];
                    b.transform.localPosition = new Vector3(y, x, 0);
                    b.transform.localScale = new Vector3(packedDiameterWS, packedDiameterWS, packedDiameterWS);
                    b.gameObject.SetActive(true);
                    if (y > bubblesWidthWS) bubblesWidthWS = y;
                    if (x > bubblesHeightWS) bubblesHeightWS = x;
                    x += diameterWS;
                }
                y += radiusWS;
            }

            bubblesWidthWS += packedRadiusWS;
            bubblesHeightWS += packedRadiusWS;
        }

        // Center grid based on dimensions.
        {
            Vector3 bubblesRootPos = bubblesRoot.transform.localPosition;
            bubblesRootPos.x = -bubblesWidthWS * 0.5f;
            bubblesRootPos.y = -bubblesHeightWS * 0.5f;
            bubblesRoot.transform.localPosition = bubblesRootPos;
        }

        if (isGameSession)
        {
            // Randomly distribute bombs.
            DistributeBombs((int)(grid.Length * bombRatio));
        }
    }

    void InstantiateGridObjects()
    {
        if (grid != null)
        {
            for (int i = 0; i < grid.Length; ++i)
            {
                Destroy(grid[i].gameObject);
            }
            grid = null;
        }

        int bubbleCount = hCount * vCount;
        grid = new Bubble[bubbleCount];

        Transform bubblesRootXform = bubblesRoot.transform;
        for (int y = 0; y < vCount; ++y)
        {
            for (int x = 0; x < hCount; ++x)
            {
                // TODO: Make this async.
                Bubble newBubble = Instantiate<Bubble>(bubblePrefab, bubblesRootXform);
                newBubble.gameObject.SetActive(false);
                newBubble.gridX = x;
                newBubble.gridY = y;
                newBubble.board = this;
                grid[y * hCount + x] = newBubble;
            }
        }
    }

    bool TryIncreaseBombCount(int x, int y)
    {
        int xMax = ((y & 1) == 0) ? hCount : hCount - 1;
        if (y >= 0 && y < vCount && x >= 0 && x < xMax)
        {
            grid[y * hCount + x].neighborBombCount += 1;
            return true;
        }
        return false;
    }

    bool TryDecreaseBombCount(int x, int y)
    {
        int xMax = ((y & 1) == 0) ? hCount : hCount - 1;
        if (y >= 0 && y < vCount && x >= 0 && x < xMax)
        {
            if (grid[y * hCount + x].neighborBombCount > 0)
            {
                grid[y * hCount + x].neighborBombCount -= 1;
            }
            return true;
        }
        return false;
    }

    private void RemoveBomb(Bubble b)
    {
        int x = b.gridX;
        int y = b.gridY;

        Debug.Assert(b.isBomb);

        b.isBomb = false;
        // In all current use cases we want to block
        // this bubble from getting a bomb again if we remove it.
        b.blockBombDistribution = true;

        // Remove bomb.
        TryDecreaseBombCount(x, y + 2); // Up
        TryDecreaseBombCount(x, y - 2); // Down
        TryDecreaseBombCount(x - 1, y); // Left
        TryDecreaseBombCount(x + 1, y); // Right
        if ((y & 1) == 0)
        {
            // If even row.
            TryDecreaseBombCount(x - 1, y + 1); // Up left
            TryDecreaseBombCount(x, y + 1); // Up right
            TryDecreaseBombCount(x - 1, y - 1); // Bottom left
            TryDecreaseBombCount(x, y - 1); // Bottom right
        }
        else
        {
            // If odd row.
            TryDecreaseBombCount(x, y + 1); // Up left
            TryDecreaseBombCount(x + 1, y + 1); // Up right
            TryDecreaseBombCount(x, y - 1); // Bottom left
            TryDecreaseBombCount(x + 1, y - 1); // Bottom right
        }
    }

    int TryRemoveBomb(int x, int y)
    {
        int xMax = ((y & 1) == 0) ? hCount : hCount - 1;
        if (y >= 0 && y < vCount && x >= 0 && x < xMax)
        {
            // Assuming we're not exploded otherwise
            // game would already be over.
            Bubble b = grid[y * hCount + x];
            if (b.isBomb)
            {
                RemoveBomb(b);
                return 1;
            }
        }
        return 0;
    }

    private int RemoveBombFromNeighbors(Bubble b)
    {
        int x = b.gridX;
        int y = b.gridY;

        int removedBombCount = 0;

        // Remove bomb.
        removedBombCount += TryRemoveBomb(x, y + 2); // Up
        removedBombCount += TryRemoveBomb(x, y - 2); // Down
        removedBombCount += TryRemoveBomb(x - 1, y); // Left
        removedBombCount += TryRemoveBomb(x + 1, y); // Right
        if ((y & 1) == 0)
        {
            // If even row.
            removedBombCount += TryRemoveBomb(x - 1, y + 1); // Up left
            removedBombCount += TryRemoveBomb(x, y + 1); // Up right
            removedBombCount += TryRemoveBomb(x - 1, y - 1); // Bottom left
            removedBombCount += TryRemoveBomb(x, y - 1); // Bottom right
        }
        else
        {
            // If odd row.
            removedBombCount += TryRemoveBomb(x, y + 1); // Up left
            removedBombCount += TryRemoveBomb(x + 1, y + 1); // Up right
            removedBombCount += TryRemoveBomb(x, y - 1); // Bottom left
            removedBombCount += TryRemoveBomb(x + 1, y - 1); // Bottom right
        }

        return removedBombCount;
    }

    private void AddBomb(Bubble b)
    {
        int x = b.gridX;
        int y = b.gridY;

        b.isBomb = true;

        // Update neighbor's bomb counts.
        TryIncreaseBombCount(x, y + 2); // Up
        TryIncreaseBombCount(x, y - 2); // Down
        TryIncreaseBombCount(x - 1, y); // Left
        TryIncreaseBombCount(x + 1, y); // Right
        if ((y & 1) == 0)
        {
            // If even row.
            TryIncreaseBombCount(x - 1, y + 1); // Up left
            TryIncreaseBombCount(x, y + 1); // Up right
            TryIncreaseBombCount(x - 1, y - 1); // Bottom left
            TryIncreaseBombCount(x, y - 1); // Bottom right
        }
        else
        {
            // If odd row.
            TryIncreaseBombCount(x, y + 1); // Up left
            TryIncreaseBombCount(x + 1, y + 1); // Up right
            TryIncreaseBombCount(x, y - 1); // Bottom left
            TryIncreaseBombCount(x + 1, y - 1); // Bottom right
        }
    }

    void DistributeBombs(int bombCount)
    {
        if (grid == null)
        {
            Debug.LogError("Null grid, cannot distribute bombs.");
            return;
        }

        if (bombCount == 0) return;

        // Even chance among bubbles to drop bomb.
        // Doesn't account for jagged array, but that should be fine I think.
        // Note: (1 / totalCells) should mean we should end up picking around 1 cell on a full grid pass.
        // So (bombCount / totalCells) should mean we pick about the number of bombs we need on a single pass.
        // We should see that distribution should consistently happen in 1 or 2 loops only.
        float bombDropChance = (float)bombCount / (float)(hCount * vCount);

        Random.InitState((int)Time.realtimeSinceStartup);

        int bubbleCount = hCount * vCount;
        int placedBombs = 0;
        int loopCount = 0;
        while (placedBombs < bombCount)
        {
            ++loopCount;

            for (int y = 0; y < vCount; ++y)
            {
                for (int x = 0; x < hCount; ++x)
                {
                    // If we're an even indexed row, we're full width.
                    // If we're an odd indexed row, skip last bubble on the row.
                    if (((y & 1) == 1) && (x >= hCount - 1)) continue;

                    Bubble b = grid[y * hCount + x];
                    if (Random.value < bombDropChance 
                        && !b.isBomb 
                        && (b.state == Bubble.State.Normal) 
                        && !b.blockBombDistribution
                    ) {
                        AddBomb(b);

                        ++placedBombs;

                        if (placedBombs >= bombCount) goto LABEL_FinishedPlacingBombs;
                    }
                }
            }
        }

    LABEL_FinishedPlacingBombs:

        Debug.Log($"Distributed {placedBombs} bombs in {loopCount} loops.");
    }

    private void CheckGameWin()
    {
        // Check if all non-bomb bubbles have been popped.
        bool hasUnpoppedBubble = false;
        for (int i = 0; i < grid.Length; ++i)
        {
            if (grid[i].gameObject.activeSelf && !grid[i].isBomb && grid[i].state == Bubble.State.Normal)
            {
                hasUnpoppedBubble = true;
                break;
            }
        }
        if (!hasUnpoppedBubble)
        {
            isMouseClickOnBubblesDisabled = true;
            isGameOver = true;
            gameController.NotifyWin();
        }
    }

    bool TryPopBubbleRecursive(int x, int y)
    {
        int xMax = ((y & 1) == 0) ? hCount : hCount - 1;
        if (y >= 0 && y < vCount && x >= 0 && x < xMax)
        {
            Bubble b = grid[y * hCount + x];
            if (b.state == Bubble.State.Normal && !b.isBomb)
            {
                PopBubble(b, recursiveCall: true);
            }
            return true;
        }
        return false;
    }

    private void TryPopBubbleNeighborsRecursive(Bubble b)
    {
        int x = b.gridX;
        int y = b.gridY;

        TryPopBubbleRecursive(x, y + 2); // Up
        TryPopBubbleRecursive(x, y - 2); // Down
        TryPopBubbleRecursive(x - 1, y); // Left
        TryPopBubbleRecursive(x + 1, y); // Right
        if ((y & 1) == 0)
        {
            // If even row.
            TryPopBubbleRecursive(x - 1, y + 1); // Up left
            TryPopBubbleRecursive(x, y + 1); // Up right
            TryPopBubbleRecursive(x - 1, y - 1); // Bottom left
            TryPopBubbleRecursive(x, y - 1); // Bottom right
        }
        else
        {
            // If odd row.
            TryPopBubbleRecursive(x, y + 1); // Up left
            TryPopBubbleRecursive(x + 1, y + 1); // Up right
            TryPopBubbleRecursive(x, y - 1); // Bottom left
            TryPopBubbleRecursive(x + 1, y - 1); // Bottom right
        }
    }

    private void PopBubble(Bubble b, bool recursiveCall = false)
    {
        // Handle some special first pop logic
        // to make sure the game doesn't end
        // prematurely. It's kind of the nature of this
        // game, but it's annoying when it ends immediately.
        if (isFirstPop)
        {
            isFirstPop = false;

            if (b.isBomb || b.neighborBombCount > 0)
            {
                // Remove bomb from self and immediate neighbors.
                int removedBombCount = 0;
                if (b.isBomb)
                {
                    RemoveBomb(b);
                    removedBombCount += 1;
                }
                removedBombCount += RemoveBombFromNeighbors(b);

                // Add bombs back somewhere else.
                // Removing bombs blocks those from getting bombs again.
                if (removedBombCount > 0)
                {
                    DistributeBombs(removedBombCount);
                }
            }
        }

        if (b.isBomb)
        {
            isMouseClickOnBubblesDisabled = true;
            isGameOver = true;
            b.ExecuteExplodeEffects();
            gameController.NotifyLost();
        }
        else
        {
            b.ExecutePopEffects(noSFX: recursiveCall);

            if (isGameSession && b.neighborBombCount == 0)
            {
                TryPopBubbleNeighborsRecursive(b);
            }

            if (!recursiveCall) CheckGameWin();
        }

        isFirstPop = false;
    }
}
