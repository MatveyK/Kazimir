using System.Collections.Generic;

public class DiscreteModel {

    private Dictionary<int, List<int>> neighboursMap;

    public DiscreteModel() {
        neighboursMap = new Dictionary<int, List<int>>();
    }

    public void addCellToNeighbours(int cellId) {
        neighboursMap[cellId] = new List<int>();
    }

}
