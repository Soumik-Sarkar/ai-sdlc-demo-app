import { expect, type Locator, type Page } from '@playwright/test';

/** Page object for the invoice app: sign-in, creating invoices and the Approvals tab. */
export class InvoiceAppPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/');
  }

  async signIn(username: string, password = 'ChangeMe123!') {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  }

  async createInvoice(customer: string, amount: number) {
    await this.page.getByLabel('Customer name').fill(customer);
    await this.page.getByLabel('Amount').fill(String(amount));
    await this.page.getByRole('button', { name: 'Create invoice' }).click();
    await expect(this.invoiceRow(customer)).toBeVisible();
  }

  async submitForApproval(customer: string) {
    await this.invoiceRow(customer).getByRole('button', { name: /Submit invoice .* for approval/ }).click();
    await expect(this.invoiceRow(customer)).toContainText('Pending Approval');
  }

  invoiceRow(customer: string): Locator {
    return this.page.getByRole('row').filter({ hasText: customer });
  }

  async openApprovalsTab() {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
  }

  async openInvoicesTab() {
    await this.page.getByRole('tab', { name: 'Invoices' }).click();
  }

  pendingItem(customer: string): Locator {
    return this.page.getByRole('listitem').filter({ hasText: customer });
  }

  async approve(customer: string) {
    await this.pendingItem(customer).getByRole('button', { name: /^Approve invoice/ }).click();
  }

  decisionError(): Locator {
    return this.page.getByRole('alert').filter({ hasText: /./ });
  }
}
