namespace Dictionary.Shared
{
    public class Node : IComparable
    {
        public string? Name { get; set; }
        public List<DictionaryEntry> DictionaryEntries { get; set; }
        public List<Node>? Children = new();
        public Node? Parent;
        public bool HasPropagated = false;
        public float X = 0;
        public float Mod = 0;
        public int Y = 0;
        public SortType mySortType = SortType.Alphabetical;

        public Node(string name, List<DictionaryEntry> dictionaryEntries, Node parent)
        {
            Name = name;
            dictionaryEntries.Sort();
            DictionaryEntries = dictionaryEntries;
            Parent = parent;
        }
        public override string ToString()
        {
            if (Name == "")
            {
                return "Dictionary /n" + GenderRatios[0].ToString("P0") + " " + GenderRatios[1].ToString("P0") + " " + GenderRatios[2].ToString("P0");
            }
            var enam = Name.ToCharArray();
            Array.Reverse(enam);
            return new string(enam) + " /n" + GenderRatios[0].ToString("P0") + " " + GenderRatios[1].ToString("P0") + " " + GenderRatios[2].ToString("P0");
        }
        public float[] GenderRatios =>
                new float[] {
                (float)CountGenders[0] / DictionaryEntries.Count,
                (float)CountGenders[1] / DictionaryEntries.Count,
                (float)CountGenders[2] / DictionaryEntries.Count
                };
        public int[] CountGenders =>
                new int[]{
                DictionaryEntries.Count(c => c.Gender == DictionaryEntry.GenderEnum.der),
                DictionaryEntries.Count(c => c.Gender == DictionaryEntry.GenderEnum.die),
                DictionaryEntries.Count(c => c.Gender == DictionaryEntry.GenderEnum.das)
                };
        public int CountChildNodes()
        {
            int counter = 0;
            if (Children is not null)
            {
                foreach (Node child in Children)
                {
                    counter++;
                    counter += CountChildNodes();
                }
            }
            return counter;
        }
        public void Propagate(int maxDepth, int cullNodesUnder)
        {
            if (DictionaryEntries.Count > 1) //Not full word which is not part of another word. (Case 4)
            {
                if (GenderRatios.Contains(1)) //Case 2: words of the present node are all the same gender (or there's only one left). We are finished.
                {
                    //What do we do in this case? Actually, we shouldn't reach this case, because such a node
                    //shouldn't have propagate called on it.
                }
                else
                {
                    bool homographs = false;
                    string lastWord = "";
                    Char lastChar = ' ';
                    Node? myNode = null;
                    Node? alternateNode = null;
                    Node? alternate2Node = null;
                    bool firstCycle = true;

                    foreach (DictionaryEntry entry in DictionaryEntries)
                    {
                        Char currentChar = ' ';
                        try
                        {
                            currentChar = entry.Reversed().ToLower()[Name.Length];
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            currentChar = ' ';
                            break;
                        }
                        if (currentChar.ToString().CompareTo(lastChar.ToString()) != 0) // create new node - we haven't seen this character yet. Also unfuck shit (no case 3 anymore.)
                        {
                            if (!firstCycle) // Finish off previous node since we're now sure it's done, unless we're just getting started.
                            {
                                bool merged = tryLeftMergeNode(myNode); //Is this a mergeable node? (Guaranteed no children, same gender as the last node.)
                                if(!merged && myNode.DictionaryEntries.Count > cullNodesUnder)
                                {
                                    if(maxDepth > myNode.Name.Length)
                                    {
                                        myNode.Propagate(maxDepth, cullNodesUnder);
                                    }
                                    Children.Add(myNode.ReturnSignificantNode());
                                }
                            }
                            firstCycle = false;
                            myNode = new Node(Name + currentChar, new List<DictionaryEntry> { entry }, this);
                            lastWord = entry.Reversed();
                            lastChar = currentChar;
                            homographs = false;
                        }
                        else // HOMOGRAPH handling - no worry about handling insignificant nodes, don't propagate further.
                        {

                            myNode.DictionaryEntries.Add(entry);
                        }
                        if(entry == DictionaryEntries.Last())
                        {
                            bool merged = tryLeftMergeNode(myNode); //Is this a mergeable node? (Guaranteed no children, same gender as the last node.)
                            if (!merged && myNode.DictionaryEntries.Count > cullNodesUnder)
                            {
                                if (maxDepth > myNode.Name.Length)
                                {
                                    myNode.Propagate(maxDepth, cullNodesUnder);
                                }
                                Children.Add(myNode.ReturnSignificantNode());
                            }
                        }
                    }
                }
            }
        }

        private bool tryLeftMergeNode(Node node)
        {
            if(node.Parent.Children.Count > 0) {
                Node preexistingNode = node.Parent.Children.Last();
                if (Array.IndexOf(node.GenderRatios, (float)1) == -1)
                {
                    return false;
                }
                else
                {
                    if (Array.IndexOf(node.GenderRatios, (float)1) == Array.IndexOf(preexistingNode.GenderRatios, (float)1))
                    {
                        if (preexistingNode.Name[preexistingNode.Name.Length - 2] == '-')
                        {
                            char[] newName = preexistingNode.Name.ToCharArray();
                            newName[preexistingNode.Name.Length - 3] = node.Name[node.Name.Length - 1];
                            preexistingNode.Name = new string(newName);
                        }
                        else
                        {
                            string newName = preexistingNode.Name.Substring(0, preexistingNode.Name.Length-1);
                            char endChar = preexistingNode.Name[node.Name.Length - 1];
                            newName += node.Name[node.Name.Length - 1] + "-" + endChar;
                            preexistingNode.Name = newName;
                        }
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private void AddEntryIfGenderMatches(DictionaryEntry entry)
        {
            if (DictionaryEntries[0].Gender == entry.Gender)
            {
                DictionaryEntries.Add(entry);
            }
        }

        private Node ReturnSignificantNode()
        {
            if (Children.Count == 1)
            {
                if (Children[0].DictionaryEntries.Equals(DictionaryEntries))
                {
                    return Children[0].ReturnSignificantNode();
                }
            }
            return this;
        }
        public void CalcLayout(SortType sortType) // Only call once on the root node.
        {
            CalcInitialPos(0, sortType);
            // Now the nodes all have local x and mod values (for immediate children.)
            float modSum = 0;
            CalcFinalX(modSum);

        }
        private void CalcInitialPos(int y, SortType sortType)
        {
            mySortType = sortType;
            Children.Sort();

            Y = y;
            foreach (var child in Children)
                child.CalcInitialPos(y + 1, sortType);
            // if there is a previous sibling in this set,
            // set X to prevous sibling + designated distance
            if (!IsLeftMost())
            {
                X = GetPreviousSibling().X + 1;
            }
            else
            {
                // if this is the first node in a set, set X to 0
                X = 0;
            }
            Console.Write(Name + X.ToString() + " " + Mod.ToString() + " ");
            PositionOverChildren();
            Console.WriteLine(Name + X.ToString() + " " + Mod.ToString() + " ");
            if (Children.Count > 0 && !IsLeftMost())
            {
                // Since subtrees can overlap, check for conflicts and shift tree right if needed
                CheckForConflicts();
            }
        }
        private void CheckForConflicts()
        {
            var minDistance = 1;
            var shiftValue = 0F;

            var nodeContour = new Dictionary<int, float>();
            GetLeftContour(0, ref nodeContour);

            var sibling = GetLeftMostSibling();
            while (sibling != null && sibling != this)
            {
                var siblingContour = new Dictionary<int, float>();
                sibling.GetRightContour(0, ref siblingContour);

                for (int level = Y + 1; level <= Math.Min(siblingContour.Keys.Max(), nodeContour.Keys.Max()); level++)
                {
                    var distance = nodeContour[level] - siblingContour[level];
                    if (distance + shiftValue < minDistance)
                    {
                        shiftValue = Math.Max(shiftValue, Math.Max(minDistance - distance, shiftValue));
                    }
                }

                sibling = Parent.Children[Parent.Children.IndexOf(sibling) + 1];
            }
            if (shiftValue > 0)
            {
                X += shiftValue;
                Mod += shiftValue;

                //CenterNodesBetween(node, sibling);

                shiftValue = 0;
            }

        }
        private void GetLeftContour(float modSum, ref Dictionary<int, float> values)
        {
            if (!values.ContainsKey(Y))
                values.Add(Y, X + modSum);
            else
                values[Y] = Math.Min(values[Y], X + modSum);

            modSum += Mod;
            foreach (var child in Children)
            {
                child.GetLeftContour(modSum, ref values);
            }
        }
        private void GetRightContour(float modSum, ref Dictionary<int, float> values)
        {
            if (!values.ContainsKey(Y))
                values.Add(Y, X + modSum);
            else
                values[Y] = Math.Max(values[Y], X + modSum);

            modSum += Mod;
            foreach (var child in Children)
            {
                child.GetRightContour(modSum, ref values);
            }
        }
        private void CalcFinalX(float modSum)
        {
            X += modSum;
            modSum += Mod;
            Console.WriteLine(Name + X.ToString() + " " + Mod.ToString() + " ");
            foreach (var child in Children)
            {
                child.CalcFinalX(modSum);
            }
        }
        private void PositionOverChildren()
        {
            float desiredX = 0;
            if (Children.Count <= 1)
            {
                desiredX = 0;
            }
            else
            {
                desiredX = (Children[0].X + Children[Children.Count - 1].X) / 2;
            }
            if (IsLeftMost())
            {
                X = desiredX;
            }
            else
            {
                Mod = X - desiredX;
            }
        }
        private Node GetPreviousSibling()
        {
            if (Parent != null)
            {
                int index = Parent.Children.IndexOf(this) - 1;
                if (index >= 0)
                {
                    return Parent.Children[index];
                }
            }
            return this;
        }
        public Node GetLeftMostSibling()
        {
            if (Parent == null)
                return null;

            if (IsLeftMost())
                return this;

            return Parent.Children[0];
        }
        private bool IsLeftMost()
        {
            if (Parent != null)
            {
                if (Parent.Children[0] == this)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public int CompareTo(object? obj)
        {
            if (obj is not null)
            {
                if (mySortType == SortType.Masculine)
                {
                    Node node = (Node)obj;
                    return node.GenderRatios[0].CompareTo(GenderRatios[0]);
                }
                else if (mySortType == SortType.Feminine)
                {
                    Node node = (Node)obj;
                    return node.GenderRatios[1].CompareTo(GenderRatios[1]);
                }
                else if (mySortType == SortType.Neuter)
                {
                    Node node = (Node)obj;
                    return node.GenderRatios[2].CompareTo(GenderRatios[2]);
                }
                else
                {
                    Node node = (Node)obj;
                    return Name.CompareTo(node.Name);
                }
            }
            else return 0;
        }
    }
}