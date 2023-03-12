### List of the API RPC Wallet request.

This file contain every requests and their arguments of the API. 

You can retrieve stats, control wallets, send transactions by this API.

Of course, by security follow the part 4 of this tutorial to secure your API from bad incoming entry:
https://github.com/Xenophyte-coin/Xenophyte-RPC-Wallet/wiki/How-to-setting-Xenophyte-RPC-Wallet-configuration-file

<table style='font-family:"Courier New", Courier, monospace; font-size:40%'>
  <thead>
    <th>Request</th>
    <th>Argument(s)</th>
    <th>Argument(s) type(s)</th>
    <th>Result(s)</th>
  </thead>
  <tbody>
    <tr><td>/get_wallet_index</td><td></td><td></td><td>Return the amount of wallets.</tr>
    <tr><td>/get_total_transaction_sync</td><td></td><td></td><td>Return the amount of transaction(s) synced.</td></tr>
    <tr><td>/get_wallet_address_by_index|wallet_index</td><td>wallet_index</td><td>long</td><td>Return the wallet address from an index selected.</td></tr>
    <tr><td>/get_wallet_balance_by_index|wallet_index</td><td>wallet_index</td><td>long</td><td>Return the wallet balance from an index selected.</td></tr>
    <tr><td>/get_wallet_balance_by_wallet_address|wallet_address</td><td>wallet_address</td><td>string</td><td>Return the wallet balance from a wallet address selected.</td></tr>
    <tr><td>/get_wallet_total_transaction_by_index|wallet_index</td><td>wallet_index</td><td>long</td><td>Return the amount of transaction(s) from a wallet index selected.</td></tr>
    <tr><td>/get_total_anonymous_transaction_by_index|wallet_index</td><td>wallet_index</td><td>long</td><td>Return the amount of anonymous transaction(s) from a wallet index selected.</td></tr>
    <tr><td>/get_wallet_total_transaction_by_wallet_address|wallet_address</td><td>wallet_address</td><td>string</td><td>Return the amount of transaction(s) from a wallet address selected.</td></tr>
    <tr><td>/get_total_anonymous_transaction_by_wallet_address|wallet_address</td><td>wallet_address</td><td>string</td><td>Return the amount of transaction(s) from a wallet address selected.</td></tr>
    <tr><td>/get_wallet_transaction|transaction_index|wallet_address</td><td>transaction_index|wallet_address</td><td>long | string</td><td>Return a transaction from a transaction index and a wallet address selected.</td></tr>
    <tr><td>/get_wallet_anonymous_transaction|transaction_index|wallet_address</td><td>transaction_index|wallet_address</td><td>long | string</td><td>Return an anonymous transaction from a transaction index and a wallet address selected.</td></tr>
    <tr><td>/get_whole_wallet_transaction_by_range|start_index|end_index</td><td>start_index|end_index</td><td>long | long</td><td>Return multiple transaction(s) from a range.</td></tr>
    <tr><td>/get_wallet_transaction_by_hash|wallet_address|transaction_hash</td><td>transaction_hash</td><td>string</td><td>Return a wallet transaction from a transaction hash linked to a wallet address.</td></tr>
    <tr><td>/get_transaction_by_hash|transaction_hash</td><td>wallet_address|transaction_hash</td><td>string</td><td>Return a wallet transaction from his transaction hash.</td></tr>
    <tr><td>/send_transaction_by_wallet_address|wallet_address_src|amount|fee|anonymous_option|wallet_address_target</td><td>wallet_address_src|amount|fee|anonymous_option|wallet_address_target</td><td>string | double | double | int | string</td><td>Send a transaction from a wallet source to a wallet address target.</td></tr>
    <tr><td>/send_transfer_by_wallet_address|wallet_address_src|amount|wallet_address_target</td><td>wallet_address_src|amount|wallet_address_target</td><td>string | double | string</td><td>Send a transfer from a wallet to another one inside of the RPC Wallet.</td></tr>
    <tr><td>/task_send_transaction|wallet_address_src|amount|fee|anonymous_option|wallet_address_target</td><td>wallet_address_src|amount|fee|anonymous_option|wallet_address_target</td><td>string | double | double | int | string</td><td>Create a task to send a transaction from a wallet to a wallet address target.</td></tr>
    <tr><td>/task_send_transfer|wallet_address_src|amount|wallet_address_target</td><td>wallet_address_src|amount|wallet_address_target</td><td>string | double | string</td><td>Create a task to send a transfer from a wallet to another one inside of the RPC Wallet.</td></tr>
    <tr><td>/update_wallet_by_address|wallet_address</td><td>wallet_address</td><td>string</td><td>Force the update of a wallet from a wallet address.</td></tr>
    <tr><td>/update_wallet_by_index|wallet_index</td><td>wallet_index</td><td>long</td><td>Force the update of a wallet from an index.</td></tr>
    <tr><td>/create_wallet|timeout</td><td>timeout</td><td>int</td><td>Create a wallet, return the wallet address generated.</td></tr>
  </tbody>
 </table>
 
