## 📝 Description
<!-- Clear description of what this PR does -->

## 🎯 Related Issues
<!-- Link to issues this PR closes or relates to -->
Closes #123
Relates to #456

## 🔄 Changes
<!-- List the main changes this PR introduces -->
- Change 1
- Change 2
- Change 3

## 🧪 Testing
<!-- How did you test this change? -->

- [ ] Tested in polling mode (dev)
- [ ] Tested in webhook mode (if applicable)
- [ ] Verified Persian text displays correctly
- [ ] Confirmed price formatting with `FormatToman()`
- [ ] Tested edge cases:
  - [ ] Empty results
  - [ ] Out of stock
  - [ ] Zero discounts
  - [ ] Concurrent operations

### Manual Test Checklist
- [ ] Feature works end-to-end
- [ ] No console errors
- [ ] Database changes persisted correctly
- [ ] Redis state managed correctly (if applicable)

### Screenshots / Evidence
<!-- If UI changes, attach before/after screenshots -->

## 📋 Checklist

- [ ] Code follows project style guide (PascalCase, `Async` suffix, etc.)
- [ ] No hardcoded English text (all Persian in `BotMessages.cs`)
- [ ] All prices use `PriceFormatter.FormatToman()`
- [ ] No secrets committed (bot token, connection strings, etc.)
- [ ] Architecture rules respected:
  - [ ] No Domain layer dependencies
  - [ ] Application imports Domain only
  - [ ] Telegram handlers in Bot layer
- [ ] Proper error handling with Persian error messages
- [ ] Database migrations created (if schema changes)
- [ ] No breaking changes (or documented if intentional)

## 🚀 Deployment Notes
<!-- Any special instructions for deploying this change? -->
- Running migrations? Include command
- Environment variables needed?
- Configuration changes?

## 💬 Additional Notes
<!-- Anything else reviewers should know? -->
