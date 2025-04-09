using System.Text;
using NUnit.Framework;
using Serilog;

namespace test.playwright.framework.coding;

public class Coding
{
    [Test]
    public void PracticeRunner()
    {
        string palindromeWord = "racecar";
        string randomWords = "Hello, world! 123";
        List<string> fruitsList = ["apple", "banana", "orange", "strawberry"];
        List<int> fruitsInt = [521, 2, 3, 4, 12, 41];
        List<int> randomNumbers = [1, 2, 3, 1, 4, 6, 2, 6, 8, 9];
        List<int> randomNumbers2 = [3, 2, 3, 1, 4, 6, 2, 6, 8, 9];
        int[] numbers = [11, 2, 3, 4, 5, 1, 7, 9, 8];
        Console.WriteLine(Practice.CountWords(fruitsList));
        Console.WriteLine(Practice.FindLargest(fruitsInt));
        Console.WriteLine(Practice.FindSmallest(fruitsInt));
        Console.WriteLine(Practice.FindSumOfNumbers(fruitsInt));
        Console.WriteLine(string.Join(", ", Practice.ReverseArrayOfIntegers(fruitsInt)));
        Console.WriteLine(Practice.IsPalindrome(palindromeWord));
        Console.WriteLine(Practice.IsPalindromeManual(palindromeWord));
        Console.WriteLine(string.Join(", ", Practice.FindAndReturnEvenNumbersList(fruitsInt)));
        Console.WriteLine(string.Join(", ", Practice.FindAndReturnMatchingLetterWords(fruitsList)));
        Console.WriteLine(string.Join(", ", Practice.SecondOption_FindAndReturnMatchingLetterWords(fruitsList, 'O')));
        Console.WriteLine(string.Join(", ", Practice.FindAndReturnDuplicateNumbers(randomNumbers)));
        Console.WriteLine(string.Join(", ", Practice.FindAndReturnDuplicateNumbersManual(randomNumbers2)));
        var charFreq = Practice.CountCharsInWord(palindromeWord);
        foreach (var entry in charFreq)
        {
            Console.WriteLine(entry);
        }

        var charFreq2 = Practice.CountOnlyLetters(randomWords);
        foreach (var entry in charFreq2)
        {
            Console.WriteLine($"Here are results: {entry.Key}: {entry.Value}");
        }

        Console.WriteLine(string.Join(", ", Practice.BubbleSortArray(numbers)));
    }

    public static class Practice
    {
        //List:
        private static readonly List<string> FruitsList = ["apple", "banana", "orange", "strawberry"];

        // Looping through list
        public static void LoopThroughFruits()
        {
            foreach (var fruit in FruitsList)
            {
                Console.WriteLine(fruit);
            }
        }

        // Using if/else condition
        public static void CheckIfBananaExists()
        {
            Console.WriteLine(FruitsList.Contains("banana") ? "Banana is present!" : "Banana is not found!");
        }

        // Reversing a string manually
        public static void HowToReverseString()
        {
            const string word = "hello";
            char[] charArray = word.ToCharArray();
            Array.Reverse(charArray);
            string reversedWord = new string(charArray);

            Console.WriteLine(reversedWord);
        }

        /*Reverse string manually*/

        public static string ReverseStringWordManually(string word)
        {
            string reversedWord = "";

            for (int i = word.Length - 1; i >= 0; i--)
            {
                reversedWord += word[i];
            }

            return reversedWord;
        }

        /*Reverse string manually using StringBuilder*/

        public static string ReverseStringUsingStringBuilder(string word)
        {
            var reversedWord = new StringBuilder();

            for (int i = word.Length - 1; i >= 0; i--)
            {
                reversedWord.Append(word[i]);
            }

            return reversedWord.ToString();
        }

        // Checking even number
        public static bool IsEven(int number)
        {
            return number % 2 == 0;
        }


        /*Find the largest number in  a list:
            1. Assume first number is biggest ->
            2. Loop through all numbers ->
            3. If another number is bigger, replace.*/

        public static int FindLargest(List<int> numbers)
        {
            int largest = numbers[0];

            foreach (var number in numbers)
            {
                if (number > largest)
                {
                    largest = number;
                }
            }

            return largest;
        }

        /*Find the smallest number in  a list:
            1. Assume first number is smallest ->
            2. Loop through all numbers ->
            3. If another number is smaller, replace.*/

        public static int FindSmallest(List<int> numbers)
        {
            int smallest = numbers[0];

            foreach (var number in numbers)
            {
                if (number < smallest)
                {
                    smallest = number;
                }
            }

            return smallest;
        }

        /*Count how many times a word appears in a list
            1. Start count  = 0
            2. Loop
            3. if item == Apple, increase count*/

        public static int CountWords(List<string> fruits)
        {
            int count = 0;
            foreach (var fruit in fruits)
            {
                if (fruit == "apple")
                {
                    count++;
                }
            }

            return count;
        }

        /*Find the Sum of All numbers in a List
            1. Start with declaring a sum variable. Initially = 0
            2. Loop through the list
            3. add each number with the sum value*/

        public static int FindSumOfNumbers(List<int> numbers)
        {
            int sum = 0;

            foreach (var number in numbers)
            {
                sum += number;
            }

            return sum;
            //fancy solution: int sum = numbers.Sum();
        }

        /*Reverse an Array of Integers
            1. Create an empty Array to store reverse order
            2. Loop the original list from the end to beginning
            3. add each number to the new list

            i = numbers.Count - 1 ➔ because indexing starts at 0, last index = count - 1
            i >= 0 ➔ keep going until the first element
            i-- ➔ move backward*/

        public static List<int> ReverseArrayOfIntegers(List<int> numbers)
        {
            List<int> reversedList = new List<int>();

            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                reversedList.Add(numbers[i]);
            }

            return reversedList;
        }

        /*Check if a Word is a Palindrome (the word reads the same backward as forward)
            1. Create an empty Array to store reverse order
            2. verify if the reverse equal the original word
            3. return the string if the condition is true*/

        public static bool IsPalindrome(string str)
        {
            char[] charArray = str.ToCharArray();
            Array.Reverse(charArray);
            string reversedWord = new string(charArray);

            if (!reversedWord.Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error("reversed word is not a palindrome. Try another word.");
                return false;
            }

            Log.Information("reversed word is a palindrome.");
            return true;
        }

        public static bool IsPalindromeManual(string str)
        {
            string reversedStr = "";

            for (int i = str.Length - 1; i >= 0; i--)
            {
                reversedStr += str[i];
            }

            return reversedStr.Equals(str, StringComparison.OrdinalIgnoreCase);
        }

        /*Find All Even Numbers from a List
            1. Create an empty list to store an even numbers
            2. loop through the list
            3. add condition statement if even then return and add to an empty list
            4. return the list of even numbers

            alternative and quick solution: List<int> evenNumbersList = numbers.Where(x => x % 2 == 0).ToList();*/

        public static List<int> FindAndReturnEvenNumbersList(List<int> numbers)
        {
            List<int> evenNumbersList = new List<int>();

            foreach (var number in numbers)
            {
                if (number % 2 == 0)
                {
                    evenNumbersList.Add(number);
                }
                else
                {
                    Log.Error("number is odd, try another number.");
                }
            }

            Log.Information("even numbers are: " + string.Join(", ", evenNumbersList));
            return evenNumbersList;
        }

        /*Find and return words that start with a specific letter from the List
           1. Create an empty List to store all matching words
           2. loop through the List
           2. verify if the word matches the starting letter word
           3. return the word if the condition is true*/

        public static List<string> FindAndReturnMatchingLetterWords(List<string> words)
        {
            List<string> matchingWords = new List<string>();

            foreach (var word in words)
            {
                if (word.StartsWith("a", StringComparison.OrdinalIgnoreCase))
                {
                    matchingWords.Add(word);
                }
                else
                {
                    Log.Warning("word doesn't start with a letter 'a', try another word.");
                }
            }

            return matchingWords;
        }

        public static List<string> SecondOption_FindAndReturnMatchingLetterWords(List<string> words, char letter)
        {
            List<string> matchingWords = new List<string>();

            foreach (var word in words)
            {
                if (char.ToLower(word[0]).Equals(char.ToLower(letter)))
                {
                    matchingWords.Add(word);
                }
                else
                {
                    Log.Warning($"word doesn't start with a letter {letter}, try another word.");
                }
            }

            return matchingWords;
        }

        /*Find duplicate numbers in a List
           1. Create an empty List to store all duplicates
           2. loop through the List
           2. verify if the number matches the previous number
           3. return all matching numbers in a List*/

        public static List<int> FindAndReturnDuplicateNumbers(List<int> numbers)
        {
            HashSet<int> duplicates = new HashSet<int>();
            HashSet<int> seen = new HashSet<int>();

            foreach (var number in numbers)
            {
                if (seen.Contains(number))
                {
                    duplicates.Add(number);
                    Log.Information("Found the same number, adding to duplicate numbers.");
                }
                else
                {
                    seen.Add(number);
                    Log.Information("The found number doesn't have duplicate numbers. Skipping...");
                }
            }

            return duplicates.ToList();
        }

        /*An alternative way - more manual and complex*/

        public static List<int> FindAndReturnDuplicateNumbersManual(List<int> numbers)
        {
            List<int> duplicates = new List<int>();

            for (int i = 0; i < numbers.Count; i++)
            {
                for (int j = i + 1; j < numbers.Count; j++)
                {
                    if (numbers[i] == numbers[j])
                    {
                        if (!duplicates.Contains(numbers[i]))
                        {
                            duplicates.Add(numbers[i]);
                        }
                    }
                }
            }

            Console.WriteLine(string.Join(", ", duplicates));
            return duplicates;
        }

        /*Count the frequency of each character in a String:
            1. Create a Dictionary to store count
            2. create a count variable for repeated char
            3. Loop through the
            3. return a dictionary values*/

        public static Dictionary<char, int> CountCharsInWord(string word)
        {
            char[] charArray = word.ToCharArray();
            Dictionary<char, int> countChars = new Dictionary<char, int>();

            foreach (var letter in charArray)
            {
                if (!countChars.ContainsKey(letter))
                {
                    countChars[letter] = 1;
                }
                else
                {
                    countChars[letter]++;
                }
            }

            return countChars;
        }

        /*Count only letters (ignore punctuation, spaces, numbers):*/

        public static Dictionary<char, int> CountOnlyLetters(string word)
        {
            Dictionary<char, int> countChars = new Dictionary<char, int>();

            foreach (var letter in word.ToLower())
            {
                if (char.IsLetter(letter) && !letter.Equals('_'))
                {
                    if (!countChars.ContainsKey(letter))
                    {
                        countChars[letter] = 1;
                    }
                    else
                    {
                        countChars[letter]++;
                    }
                }
            }

            return countChars;
        }

        /*Find an average*/

        public static int FindAverage(List<int> numbers)
        {
            int sum = 0;
            int average = 0;

            foreach (var number in numbers)
            {
                sum += number;
                average = sum / numbers.Count;
            }

            return average;
        }

        /*Find a max value in a complex array*/

        public static int FindMaxArray(int[][] numbers)
        {
            int max = numbers[0][0];

            for (int i = 1; i < numbers.Length; i++)
            {
                for (int j = 1; j < numbers[i].Length; j++)
                {
                    if (numbers[i][j] > max)
                    {
                        max = numbers[i][j];
                    }
                }
            }

            return max;
        }

        /*Make a bubble sort in array*/

        public static int[] BubbleSortArray(int[] numbers)
        {
            bool swapped = false;

            while (!swapped)
            {
                swapped = true;

                for (int i = 1; i < numbers.Length; i++)
                {
                    if (numbers[i - 1] > numbers[i])
                    {
                        (numbers[i - 1], numbers[i]) = (numbers[i], numbers[i - 1]);
                        swapped = false;
                    }
                }
            }

            return numbers;
        }

        /*Find a sum of numbers in a list that would be equal to the external number
            1. find the right indexes from the list
            2. verify they're matching the target number
            3. return list indexes*
            For each number:
            Calculate what you need.
            Check if you already have what you need.
            If yes, done!
            If no, save this number.*/

        public static int[] TwoSumBruteForce(int[] numbers, int target)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                for (int j = 0; j < numbers.Length; j++)
                {
                    if (numbers[i] + numbers[j] == target)
                    {
                        return [i, j];
                    }
                }
            }

            return [];
        }

        /*FizzBuzz*/

        public static void FindAndReturnFizzBuzz(int fizz, int buzz)
        {
            Log.Information("FizzBuzz starting...");
            for (int i = 1; i <= 100; i++)
            {
                if (i % fizz == 0 && i % buzz == 0)
                {
                    Console.WriteLine("FizzBuzz");
                }
                else if (i % fizz == 0)
                {
                    Console.WriteLine("Fizz");
                }
                else if (i % buzz == 0)
                {
                    Console.WriteLine("Buzz");
                }
                else
                {
                    Console.WriteLine(i);
                    Log.Information($"Number {i} is neither divisible by {fizz} nor {buzz}.");
                }
            }
        }

        /*Merge two arrays into one sorted*/

        public static List<int> MergeAndReturnSingleArray(List<int> num1, List<int> num2)
        {
            List<int> finalArray = new List<int>();
            int i = 0;
            int j = 0;

            while (i < num1.Count && j < num2.Count)
            {
                if (num1[i] < num2[j])
                {
                    finalArray.Add(num1[i]);
                    i++;
                }
                else
                {
                    finalArray.Add(num2[j]);
                    j++;
                }
            }

            while (i < num1.Count)
            {
                finalArray.Add(num1[i]);
                i++;
            }

            while (j < num2.Count)
            {
                finalArray.Add(num2[j]);
                j++;
            }

            return finalArray;
        }
    }
}