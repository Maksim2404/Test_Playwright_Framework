Feature: Project Creation

As a user
I want to create, verify, and delete project
So that I can manage project efficiently

    Scenario: Create, verify, and delete a project
        Given I navigate to the Titles List page
        When I select a random title
        And I click the Add Project button
        And I fill in the project creation form with random values
          | Property        | Values       |
          | Work Type       | Type1, Type2 |
          | Number of parts | 1            |
          | Another Value   | 1, 2, 3, 4   |
        And I submit the project creation form
        Then the project should be successfully created and I can verify its properties
        When I delete the project
        Then the project should no longer be listed under the selected title