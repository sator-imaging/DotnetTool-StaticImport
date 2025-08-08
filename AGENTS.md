# All PRs SHALL adhere to one of the following categories and comply with the respective rules.

1. Feature Update
    - SHALL include only functional changes (e.g., adding, modifying, or removing features).
    - SHALL NOT include any non-functional changes, such as:
        - Removing comments
        - Changing access modifiers (e.g., public/private)
        - Exposing local/internal functions
        - Modifying or removing #pragma or other compiler directives
        - Pure formatting or whitespace changes
2. Security Update
    - SHALL include only security-related fixes or improvements.
3. Optimization
    - SHALL include performance or resource usage improvements that DO NOT alter functionality.
    - SHALL ensure that the optimized code is already covered by existing tests.
        1. If NOT covered by tests, DO NOT create an optimization PR.
        2. Instead, first submit a test coverage update PR with no optimization changes included.
4.  Other Changes
    - Covers all other types of changes that do not fit the above categories (e.g., documentation, refactoring).
    - SHOULD be submitted in separate PRs and clearly labeled.

NOTE:
- If your changes span multiple categories, split them into separate PRs according to category.
- Clearly state the category in the PR title (e.g., [Feature] Add user login feature, [Security] Fix token validation).
