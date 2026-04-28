#:sdk Microsoft.NET.Sdk.Web
#:package FastEndpoints@8.2.0-beta.9
#:package FastEndpoints.OpenApi@8.2.0-beta.9
#:property ManagePackageVersionsCentrally=false
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.OpenApi;
using System.Text.Json;
using System.Text.Json.Nodes;

const string DefaultWethAddress = "0x4200000000000000000000000000000000000006";
const string Permit2Address = "0x000000000022D473030F116dDEE9F6B43aC78BA3";
const string X402Permit2ProxyAddress = "0x402085c248EeA27D92E8b30b2C58ed07f9E20001";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("app.run.local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddFastEndpoints()
    .AddX402()
    .OpenApiDocument(o =>
    {
        o.DocumentName = "v1";
        o.Title = "x402 demo API";
        o.Version = "v1";

        o.ExcludeNonFastEndpoints = true;
        o.ShortSchemaNames = true;
        o.EnableJWTBearerAuth = false;
    });

var facilitatorUrl = builder.Configuration["X402:FacilitatorUrl"] ?? string.Empty;
var network = builder.Configuration["X402:Network"] ?? string.Empty;
var payTo = builder.Configuration["X402:PayTo"] ?? string.Empty;
var asset = NormalizeEvmAddress(builder.Configuration["X402:Asset"], DefaultWethAddress);
var maxTimeoutSeconds = int.TryParse(builder.Configuration["X402:MaxTimeoutSeconds"], out var parsedMaxTimeoutSeconds)
    ? parsedMaxTimeoutSeconds
    : 300;
var settlementMode = Enum.TryParse<Settle>(builder.Configuration["X402:SettlementMode"], true, out var parsedSettlementMode)
    ? parsedSettlementMode
    : Settle.AfterSuccess;

var app = builder.Build();

app.UseX402(o =>
{
    o.FacilitatorUrl = facilitatorUrl;
    o.Defaults.Network = network;
    o.Defaults.PayTo = payTo;
    o.Defaults.Asset = asset;
    o.Defaults.MaxTimeoutSeconds = maxTimeoutSeconds;
    o.SettlementMode = settlementMode;
});

app.UseFastEndpoints();
app.MapOpenApi();

app.MapGet("/", () => Results.Content(BuildTestPage(facilitatorUrl, network, payTo, asset), "text/html; charset=utf-8"));

app.Run();

static string NormalizeEvmAddress(string? value, string fallback)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return fallback;
    }

    var trimmed = value.Trim();
    if (!trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
        trimmed = $"0x{trimmed}";
    }

    return trimmed.Length == 42 && trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        ? trimmed
        : fallback;
}

string BuildTestPage(string facilitatorUrl, string network, string payTo, string asset) => $$"""
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>x402 test site</title>
    <style>
        :root {
            color-scheme: dark;
            --bg: #0b1020;
            --panel: rgba(15, 23, 42, 0.88);
            --panel-border: rgba(148, 163, 184, 0.18);
            --text: #e2e8f0;
            --muted: #94a3b8;
            --accent: #22c55e;
            --accent-strong: #16a34a;
            --warning: #f59e0b;
            --code: #111827;
        }

        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            color: var(--text);
            min-height: 100vh;
            background:
                radial-gradient(circle at top left, rgba(34, 197, 94, 0.18), transparent 34%),
                radial-gradient(circle at top right, rgba(59, 130, 246, 0.16), transparent 28%),
                linear-gradient(180deg, #060816 0%, #0b1020 48%, #040712 100%);
        }

        main {
            max-width: 1100px;
            margin: 0 auto;
            padding: 48px 20px 56px;
        }

        .hero {
            display: grid;
            gap: 18px;
            grid-template-columns: minmax(0, 1.5fr) minmax(300px, 0.9fr);
            align-items: start;
        }

        .panel {
            background: var(--panel);
            border: 1px solid var(--panel-border);
            border-radius: 20px;
            box-shadow: 0 30px 80px rgba(0, 0, 0, 0.32);
            backdrop-filter: blur(18px);
        }

        .intro {
            padding: 28px;
        }

        .kicker {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            padding: 6px 12px;
            border-radius: 999px;
            background: rgba(34, 197, 94, 0.14);
            color: #86efac;
            font-size: 12px;
            letter-spacing: 0.08em;
            text-transform: uppercase;
            margin-bottom: 18px;
        }

        h1 {
            margin: 0 0 12px;
            font-size: clamp(2.25rem, 5vw, 4.6rem);
            line-height: 0.95;
            letter-spacing: -0.05em;
        }

        .lede {
            margin: 0;
            color: var(--muted);
            max-width: 58ch;
            font-size: 1.05rem;
            line-height: 1.7;
        }

        .meta {
            display: grid;
            gap: 12px;
            margin-top: 22px;
            grid-template-columns: repeat(2, minmax(0, 1fr));
        }

        .meta div {
            padding: 14px 16px;
            border-radius: 16px;
            background: rgba(15, 23, 42, 0.72);
            border: 1px solid rgba(148, 163, 184, 0.12);
        }

        .meta span {
            display: block;
            color: var(--muted);
            font-size: 12px;
            margin-bottom: 6px;
            text-transform: uppercase;
            letter-spacing: 0.08em;
        }

        .meta code,
        pre {
            font-family: Consolas, "Liberation Mono", Menlo, monospace;
        }

        .meta code {
            font-size: 0.9rem;
            color: #f8fafc;
            word-break: break-all;
        }

        .actions {
            display: grid;
            gap: 12px;
            padding: 22px;
        }

        button {
            appearance: none;
            border: 0;
            border-radius: 14px;
            padding: 14px 16px;
            font: inherit;
            font-weight: 700;
            cursor: pointer;
            color: #052e16;
            background: linear-gradient(135deg, #4ade80, #22c55e);
            box-shadow: 0 14px 30px rgba(34, 197, 94, 0.3);
        }

        button.secondary {
            background: rgba(148, 163, 184, 0.14);
            color: var(--text);
            box-shadow: none;
            border: 1px solid rgba(148, 163, 184, 0.18);
        }

        button:hover {
            transform: translateY(-1px);
        }

        .note {
            color: var(--muted);
            font-size: 0.95rem;
            line-height: 1.6;
            margin: 0;
        }

        .result {
            margin-top: 18px;
            padding: 22px;
        }

        .result-head {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: 12px;
            margin-bottom: 14px;
        }

        .result-head h2 {
            margin: 0;
            font-size: 1rem;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            color: #cbd5e1;
        }

        pre {
            margin: 0;
            min-height: 240px;
            padding: 18px;
            border-radius: 16px;
            background: var(--code);
            overflow: auto;
            color: #e5e7eb;
            border: 1px solid rgba(148, 163, 184, 0.12);
            white-space: pre-wrap;
            word-break: break-word;
        }

        .status {
            color: var(--warning);
            font-size: 0.9rem;
        }

        @media (max-width: 860px) {
            .hero {
                grid-template-columns: 1fr;
            }

            .meta {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <main>
        <section class="hero">
            <div class="panel intro">
                <div class="kicker">x402 test site</div>
                <h1>Probe the 402 response in a browser.</h1>
                <p class="lede">
                    This page calls the protected endpoint, captures the payment challenge headers,
                    and shows you the raw 402 response so you can verify the current x402 setup.
                </p>

                <div class="meta">
                    <div>
                        <span>Endpoint</span>
                        <code>/x402/premium</code>
                    </div>
                    <div>
                        <span>Price</span>
                        <code>1 atomic unit</code>
                    </div>
                    <div>
                        <span>Network</span>
                        <code>{{JsonSerializer.Serialize(network)}}</code>
                    </div>
                    <div>
                        <span>Facilitator</span>
                        <code>{{JsonSerializer.Serialize(facilitatorUrl)}}</code>
                    </div>
                    <div>
                        <span>Pay To</span>
                        <code>{{JsonSerializer.Serialize(payTo)}}</code>
                    </div>
                    <div>
                        <span>Asset</span>
                        <code>{{JsonSerializer.Serialize(asset)}}</code>
                    </div>
                </div>
            </div>

            <aside class="panel actions">
                <button id="pay" type="button">Pay with MetaMask</button>
                <button id="probe" type="button" class="secondary">Show 402 challenge</button>
                <button id="copy" type="button" class="secondary">Copy endpoint URL</button>
                <p class="note">
                    This page uses MetaMask to sign the x402 payment in-browser.
                    The demo is configured for Base Sepolia WETH and uses Permit2, so it will wrap a tiny amount of Base Sepolia ETH first if needed.
                </p>
            </aside>
        </section>

        <section class="panel result">
            <div class="result-head">
                <h2>Response</h2>
                <div id="status" class="status">Idle</div>
            </div>
            <pre id="output">Click "Trigger 402 request" to test the endpoint.</pre>
        </section>
    </main>

    <script>
        const config = {
            endpoint: new URL("/x402/premium", window.location.origin).toString(),
            network: {{JsonSerializer.Serialize(network)}},
            asset: {{JsonSerializer.Serialize(asset)}},
            permit2Address: {{JsonSerializer.Serialize(Permit2Address)}},
            permit2ProxyAddress: {{JsonSerializer.Serialize(X402Permit2ProxyAddress)}}
        };

        const status = document.getElementById("status");
        const output = document.getElementById("output");
        const probeButton = document.getElementById("probe");
        const payButton = document.getElementById("pay");
        const copyButton = document.getElementById("copy");

        function chainIdFromNetwork(networkId) {
            const match = /^eip155:(\d+)$/i.exec(networkId ?? "");
            return match ? Number(match[1]) : null;
        }

        function toHexChainId(chainId) {
            return `0x${chainId.toString(16)}`;
        }

        function toHexQuantity(value) {
            return `0x${BigInt(value).toString(16)}`;
        }

        function randomHex32() {
            const bytes = new Uint8Array(32);
            crypto.getRandomValues(bytes);
            return `0x${Array.from(bytes, byte => byte.toString(16).padStart(2, "0")).join("")}`;
        }

        function createPermit2Nonce() {
            return BigInt(randomHex32()).toString();
        }

        function normalizeAtomicAmount(value, decimals = 6) {
            if (value == null) {
                return "0";
            }

            const text = String(value).trim();
            if (/^\d+$/.test(text)) {
                return text;
            }

            const numeric = text.startsWith("$") ? text.slice(1) : text;
            if (!/^\d+(\.\d+)?$/.test(numeric)) {
                return text;
            }

            const [wholePart, fractionPart = ""] = numeric.split(".");
            const paddedFraction = `${fractionPart}${"0".repeat(decimals)}`.slice(0, decimals);
            const whole = BigInt(wholePart) * BigInt(10 ** decimals);
            const fraction = BigInt(paddedFraction || "0");
            return (whole + fraction).toString();
        }

        function normalizePaymentRequirement(requirement) {
            const amount = normalizeAtomicAmount(requirement.amount ?? requirement.maxAmountRequired);
            return {
                ...requirement,
                amount,
                maxAmountRequired: amount
            };
        }

        function formatTokenAmount(rawAmount, decimals = 18) {
            const value = BigInt(rawAmount);
            const divisor = BigInt(10) ** BigInt(decimals);
            const whole = value / divisor;
            const fraction = value % divisor;

            if (fraction === 0n) {
                return whole.toString();
            }

            const fractionText = fraction.toString().padStart(decimals, "0").replace(/0+$/, "");
            return `${whole.toString()}.${fractionText}`;
        }

        function encodeErc20BalanceOf(ownerAddress) {
            return `0x70a08231000000000000000000000000${ownerAddress.slice(2).toLowerCase()}`;
        }

        function encodeErc20Allowance(ownerAddress, spenderAddress) {
            return `0xdd62ed3e000000000000000000000000${ownerAddress.slice(2).toLowerCase()}000000000000000000000000${spenderAddress.slice(2).toLowerCase()}`;
        }

        function encodeErc20Approve(spenderAddress, amount) {
            return `0x095ea7b3000000000000000000000000${spenderAddress.slice(2).toLowerCase()}${BigInt(amount).toString(16).padStart(64, "0")}`;
        }

        async function readErc20Balance(tokenAddress, ownerAddress) {
            const balanceHex = await window.ethereum.request({
                method: "eth_call",
                params: [
                    {
                        to: tokenAddress,
                        data: encodeErc20BalanceOf(ownerAddress)
                    },
                    "latest"
                ]
            });

            return BigInt(balanceHex);
        }

        async function readErc20Allowance(tokenAddress, ownerAddress, spenderAddress) {
            const allowanceHex = await window.ethereum.request({
                method: "eth_call",
                params: [
                    {
                        to: tokenAddress,
                        data: encodeErc20Allowance(ownerAddress, spenderAddress)
                    },
                    "latest"
                ]
            });

            return BigInt(allowanceHex);
        }

        async function readNativeBalance(ownerAddress) {
            const balanceHex = await window.ethereum.request({
                method: "eth_getBalance",
                params: [ownerAddress, "latest"]
            });

            return BigInt(balanceHex);
        }

        async function waitForTransactionReceipt(transactionHash) {
            for (let attempt = 0; attempt < 30; attempt++) {
                const receipt = await window.ethereum.request({
                    method: "eth_getTransactionReceipt",
                    params: [transactionHash]
                });

                if (receipt) {
                    return receipt;
                }

                await new Promise(resolve => setTimeout(resolve, 2000));
            }

            throw new Error(`Timed out waiting for transaction ${transactionHash} to confirm.`);
        }

        async function wrapEthForWeth(ownerAddress, amountWei) {
            const transactionHash = await window.ethereum.request({
                method: "eth_sendTransaction",
                params: [{
                    from: ownerAddress,
                    to: config.asset,
                    value: toHexQuantity(amountWei),
                    data: "0xd0e30db0"
                }]
            });

            status.textContent = "Waiting for WETH wrap transaction...";
            await waitForTransactionReceipt(transactionHash);
            return transactionHash;
        }

        async function approvePermit2ForToken(ownerAddress, tokenAddress, amountWei) {
            const transactionHash = await window.ethereum.request({
                method: "eth_sendTransaction",
                params: [{
                    from: ownerAddress,
                    to: tokenAddress,
                    data: encodeErc20Approve(config.permit2Address, amountWei)
                }]
            });

            status.textContent = "Waiting for Permit2 approval transaction...";
            await waitForTransactionReceipt(transactionHash);
            return transactionHash;
        }

        function encodeJsonBase64(value) {
            const json = JSON.stringify(value);
            const bytes = new TextEncoder().encode(json);
            let binary = "";

            for (const byte of bytes) {
                binary += String.fromCharCode(byte);
            }

            return btoa(binary);
        }

        function decodeBase64Json(value) {
            const binary = atob(value);
            const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
            return JSON.parse(new TextDecoder().decode(bytes));
        }

        async function readBody(response) {
            const text = await response.text();
            const contentType = response.headers.get("content-type") ?? "";

            if (contentType.includes("application/json")) {
                try {
                    return JSON.parse(text);
                } catch {
                    return text;
                }
            }

            return text;
        }

        function pickPaymentRequirement(paymentRequired) {
            if (Array.isArray(paymentRequired.accepts) && paymentRequired.accepts.length > 0) {
                return paymentRequired.accepts[0];
            }

            if (paymentRequired.accepted) {
                return paymentRequired.accepted;
            }

            throw new Error("No payment requirements were returned by the server.");
        }

        function buildPaymentData(address, paymentRequired, accepted) {
            const chainId = chainIdFromNetwork(accepted.network ?? config.network);
            const assetTransferMethod = accepted.extra?.assetTransferMethod ?? paymentRequired.accepts?.[0]?.extra?.assetTransferMethod ?? "eip3009";

            if (!chainId) {
                throw new Error(`Unsupported network format: ${accepted.network ?? config.network}`);
            }

            const now = Math.floor(Date.now() / 1000);
            const maxTimeoutSeconds = Number(accepted.maxTimeoutSeconds ?? paymentRequired.maxTimeoutSeconds ?? 300);
            const amount = normalizeAtomicAmount(accepted.amount ?? accepted.maxAmountRequired);

            if (assetTransferMethod === "permit2") {
                const nonce = createPermit2Nonce();
                const validAfter = String(now - 600);
                const deadline = String(now + maxTimeoutSeconds);
                const permit2Authorization = {
                    from: address,
                    permitted: {
                        token: accepted.asset,
                        amount
                    },
                    spender: config.permit2ProxyAddress,
                    nonce,
                    deadline,
                    witness: {
                        to: accepted.payTo,
                        validAfter
                    }
                };

                return {
                    chainId,
                    typedData: {
                        types: {
                            EIP712Domain: [
                                { name: "name", type: "string" },
                                { name: "chainId", type: "uint256" },
                                { name: "verifyingContract", type: "address" }
                            ],
                            PermitWitnessTransferFrom: [
                                { name: "permitted", type: "TokenPermissions" },
                                { name: "spender", type: "address" },
                                { name: "nonce", type: "uint256" },
                                { name: "deadline", type: "uint256" },
                                { name: "witness", type: "Witness" }
                            ],
                            TokenPermissions: [
                                { name: "token", type: "address" },
                                { name: "amount", type: "uint256" }
                            ],
                            Witness: [
                                { name: "to", type: "address" },
                                { name: "validAfter", type: "uint256" }
                            ]
                        },
                        primaryType: "PermitWitnessTransferFrom",
                        domain: {
                            name: "Permit2",
                            chainId,
                            verifyingContract: config.permit2Address
                        },
                        message: {
                            permitted: {
                                token: accepted.asset,
                                amount
                            },
                            spender: config.permit2ProxyAddress,
                            nonce,
                            deadline,
                            witness: {
                                to: accepted.payTo,
                                validAfter
                            }
                        }
                    },
                    paymentPayload: {
                        permit2Authorization
                    }
                };
            }

            const authorization = {
                from: address,
                to: accepted.payTo,
                value: amount,
                validAfter: String(now - 600),
                validBefore: String(now + maxTimeoutSeconds),
                nonce: randomHex32()
            };

            const domain = {
                name: accepted.extra?.name ?? paymentRequired.accepts?.[0]?.extra?.name ?? "USDC",
                version: accepted.extra?.version ?? paymentRequired.accepts?.[0]?.extra?.version ?? "2",
                chainId,
                verifyingContract: accepted.asset
            };

            return {
                chainId,
                typedData: {
                    types: {
                        EIP712Domain: [
                            { name: "name", type: "string" },
                            { name: "version", type: "string" },
                            { name: "chainId", type: "uint256" },
                            { name: "verifyingContract", type: "address" }
                        ],
                        TransferWithAuthorization: [
                            { name: "from", type: "address" },
                            { name: "to", type: "address" },
                            { name: "value", type: "uint256" },
                            { name: "validAfter", type: "uint256" },
                            { name: "validBefore", type: "uint256" },
                            { name: "nonce", type: "bytes32" }
                        ]
                    },
                    primaryType: "TransferWithAuthorization",
                    domain,
                    message: authorization
                },
                paymentPayload: {
                    authorization
                }
            };
        }

        function renderResponse(response, body) {
            const headers = {};

            for (const [key, value] of response.headers.entries()) {
                headers[key] = value;
            }

            output.textContent = JSON.stringify(
                {
                    ok: response.ok,
                    status: response.status,
                    statusText: response.statusText,
                    paymentRequired: response.headers.get("payment-required"),
                    paymentSignature: response.headers.get("payment-signature"),
                    paymentResponse: response.headers.get("payment-response"),
                    headers,
                    body
                },
                null,
                2
            );

            status.textContent = response.status === 402 ? "402 challenge received" : `HTTP ${response.status}`;
        }

        function toPlainObject(value) {
            if (!value || typeof value !== "object") {
                return value;
            }

            const result = {};
            for (const [key, entry] of Object.entries(value)) {
                if (typeof entry === "bigint") {
                    result[key] = entry.toString();
                    continue;
                }

                if (entry && typeof entry === "object" && !Array.isArray(entry)) {
                    result[key] = toPlainObject(entry);
                    continue;
                }

                result[key] = entry;
            }

            return result;
        }

        function renderError(error) {
            const details = error instanceof Error
                ? {
                    name: error.name,
                    message: error.message,
                    stack: error.stack
                }
                : toPlainObject(error);

            output.textContent = JSON.stringify(details, null, 2);
        }

        async function getPaymentRequiredResponse() {
            const response = await fetch(config.endpoint, { method: "GET" });
            const body = await readBody(response);

            if (response.status !== 402) {
                return { response, body, paymentRequired: null };
            }

            const paymentRequiredHeader = response.headers.get("PAYMENT-REQUIRED");
            if (!paymentRequiredHeader) {
                throw new Error("The response did not include a PAYMENT-REQUIRED header.");
            }

            return {
                response,
                body,
                paymentRequired: decodeBase64Json(paymentRequiredHeader)
            };
        }

        async function probe() {
            status.textContent = "Requesting...";
            output.textContent = "Loading response...";

            try {
                const result = await getPaymentRequiredResponse();
                renderResponse(result.response, result.body);
            } catch (error) {
                status.textContent = "Request failed";
                renderError(error);
            }
        }

        async function payWithMetaMask() {
            if (!window.ethereum) {
                status.textContent = "MetaMask not found";
                output.textContent = "Install MetaMask or another injected EVM wallet first.";
                return;
            }

            status.textContent = "Connecting wallet...";
            output.textContent = "Opening MetaMask...";

            try {
                const [address] = await window.ethereum.request({ method: "eth_requestAccounts" });

                const requiredChainId = chainIdFromNetwork(config.network);
                if (requiredChainId !== null) {
                    const currentChainId = Number(await window.ethereum.request({ method: "eth_chainId" }));
                    if (currentChainId !== requiredChainId) {
                        try {
                            await window.ethereum.request({
                                method: "wallet_switchEthereumChain",
                                params: [{ chainId: toHexChainId(requiredChainId) }]
                            });
                        } catch (switchError) {
                            console.warn("Unable to switch chain automatically.", switchError);
                        }
                    }
                }

                status.textContent = "Requesting 402 challenge...";
                const result = await getPaymentRequiredResponse();
                if (!result.paymentRequired) {
                    renderResponse(result.response, result.body);
                    return;
                }

                const accepted = normalizePaymentRequirement(pickPaymentRequirement(result.paymentRequired));
                const { typedData, paymentPayload: paymentDetails, chainId } = buildPaymentData(address, result.paymentRequired, accepted);

                const balance = await readErc20Balance(accepted.asset, address);
                const requiredAmount = BigInt(accepted.amount);
                if (balance < requiredAmount) {
                    const nativeBalance = await readNativeBalance(address);
                    const wrapTarget = BigInt("1000000000000");
                    const wrapAmount = nativeBalance > wrapTarget ? wrapTarget : nativeBalance / 2n;

                    if (wrapAmount <= 0n) {
                        status.textContent = "Insufficient WETH";
                        output.textContent = JSON.stringify(
                            {
                                wallet: address,
                                asset: accepted.asset,
                                balance: balance.toString(),
                                balanceDisplay: formatTokenAmount(balance, 18),
                                nativeBalance: nativeBalance.toString(),
                                nativeBalanceDisplay: formatTokenAmount(nativeBalance, 18),
                                required: requiredAmount.toString(),
                                requiredDisplay: formatTokenAmount(requiredAmount, 18),
                                message: "Add a little more Base Sepolia ETH, then retry."
                            },
                            null,
                            2
                        );
                        return;
                    }

                    status.textContent = "Wrapping ETH into WETH...";
                    output.textContent = JSON.stringify(
                        {
                            wallet: address,
                            asset: accepted.asset,
                            balance: balance.toString(),
                            balanceDisplay: formatTokenAmount(balance, 18),
                            nativeBalance: nativeBalance.toString(),
                            nativeBalanceDisplay: formatTokenAmount(nativeBalance, 18),
                            wrapAmount: wrapAmount.toString(),
                            wrapAmountDisplay: formatTokenAmount(wrapAmount, 18)
                        },
                        null,
                        2
                    );

                    const wrapHash = await wrapEthForWeth(address, wrapAmount);
                    status.textContent = "WETH wrapped";
                    output.textContent = JSON.stringify(
                        {
                            wallet: address,
                            asset: accepted.asset,
                            wrapHash,
                            wrappedAmount: wrapAmount.toString(),
                            wrappedAmountDisplay: formatTokenAmount(wrapAmount, 18),
                            message: "The wrap transaction confirmed on-chain."
                        },
                        null,
                        2
                    );
                }

                const allowance = await readErc20Allowance(accepted.asset, address, config.permit2Address);
                if (allowance < requiredAmount) {
                    status.textContent = "Approving Permit2...";
                    output.textContent = JSON.stringify(
                        {
                            wallet: address,
                            asset: accepted.asset,
                            allowance: allowance.toString(),
                            allowanceDisplay: formatTokenAmount(allowance, 18),
                            required: requiredAmount.toString(),
                            requiredDisplay: formatTokenAmount(requiredAmount, 18),
                            message: "Permit2 needs approval before the x402 payment can be signed."
                        },
                        null,
                        2
                    );

                    const approvalHash = await approvePermit2ForToken(address, accepted.asset, "0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
                    status.textContent = "Permit2 approved";
                    output.textContent = JSON.stringify(
                        {
                            wallet: address,
                            asset: accepted.asset,
                            approvalHash,
                            approvalAmount: "max",
                            message: "The approval transaction confirmed on-chain."
                        },
                        null,
                        2
                    );
                }

                status.textContent = "Signing payment in MetaMask...";
                const signature = await window.ethereum.request({
                    method: "eth_signTypedData_v4",
                    params: [address, JSON.stringify(typedData)]
                });

                const signedPayment = {
                    x402Version: result.paymentRequired.x402Version ?? 2,
                    accepted,
                    payload: {
                        signature,
                        ...paymentDetails
                    },
                    resource: result.paymentRequired.resource,
                    extensions: result.paymentRequired.extensions
                };

                const paymentHeader = encodeJsonBase64(signedPayment);

                status.textContent = `Submitting payment for chain ${chainId}...`;
                const paidResponse = await fetch(config.endpoint, {
                    method: "GET",
                    headers: {
                        "PAYMENT-SIGNATURE": paymentHeader,
                        Accept: "application/json"
                    }
                });

                const paidBody = await readBody(paidResponse);
                renderResponse(paidResponse, paidBody);
            } catch (error) {
                status.textContent = "Payment failed";
                renderError(error);
            }
        }

        probeButton.addEventListener("click", probe);
        payButton.addEventListener("click", payWithMetaMask);
        copyButton.addEventListener("click", async () => {
            await navigator.clipboard.writeText(config.endpoint);
            status.textContent = "Endpoint copied";
        });
    </script>
</body>
</html>
""";

public sealed record PremiumResponse(string Message, string LoremIpsum, JsonObject Details);

public sealed class PremiumEndpoint : EndpointWithoutRequest<PremiumResponse>
{
    public override void Configure()
    {
        Get("/x402/premium");
        AllowAnonymous();
        RequirePayment(
            "1",
            "x402 demo access",
            o => o.Extra = new JsonObject
            {
                ["assetTransferMethod"] = "permit2"
            });
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(
            new PremiumResponse(
                "x402 payment accepted",
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                new JsonObject
                {
                    ["kind"] = "placeholder",
                    ["object"] = new JsonObject
                    {
                        ["name"] = "demo",
                        ["enabled"] = true
                    }
                }),
            cancellation: ct);
}

