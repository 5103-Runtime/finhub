@regression @userauthentication
Feature: User Authentication Verification

As a QA engineer
I want to test the login page's user authentication functionality with different account types
So that I can confirm that user authentication functionality works as expected for various scenarios

Background:
  Given I am on the login page

@smoke
Scenario: Logging in as user is successful
  When I log in as user
  Then I should be logged in successfully