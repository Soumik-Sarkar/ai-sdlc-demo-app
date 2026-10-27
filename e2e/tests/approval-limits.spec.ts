import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/InvoiceAppPage';

test('a Manager cannot approve an invoice above the 50,000 limit and the invoice stays pending', async ({ page }) => {
  const app = new InvoiceAppPage(page);
  // Unique customer so the test does not depend on, or disturb, other invoices.
  const customer = `Over Limit Co ${Date.now()}`;

  await app.open();
  await app.signIn('demo.manager');
  await app.createInvoice(customer, 60000);
  await app.submitForApproval(customer);

  await app.openApprovalsTab();
  await app.approve(customer);

  // The refusal gives a clear reason that names the limit and who can approve it.
  await expect(app.decisionError()).toContainText('A Manager can approve up to 50,000.00');
  await expect(app.decisionError()).toContainText('A Director must approve it');

  // Nothing changed: still waiting in the pending list, and still Pending Approval on the Invoices tab.
  await expect(app.pendingItem(customer)).toBeVisible();
  await app.openInvoicesTab();
  await expect(app.invoiceRow(customer)).toContainText('Pending Approval');
  await expect(app.invoiceRow(customer)).not.toContainText('Approved');
});
