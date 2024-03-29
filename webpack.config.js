// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

var isProduction = !hasArg(/webpack-dev-server/);

var HtmlWebpackPlugin = require('html-webpack-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = [
    {
        mode: isProduction ? 'production' : 'development',
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        entry: "./src/App.fsproj",
        output: {
            path: isProduction ? path.join(__dirname, "./deploy") : path.join(__dirname, "./public"),
            filename: "[contenthash].innit.js",
        },
        devServer: {
            publicPath: "/",
            contentBase: "./public",
            port: 8080,
            host: '192.168.1.145',
            allowedHosts: [
                'localhost',
                '0.0.0.0',
                '192.168.1.145'
            ]
        },
        module: {
            rules: [{
                test: /\.fs(x|proj)?$/,
                use: "fable-loader"
            }]
        },
        plugins: [
            new HtmlWebpackPlugin({
                filename: 'index.html',
                template: 'src/index.html'
            })
        ].concat(isProduction ? [
            new CopyWebpackPlugin({
                patterns: [{
                    from: "./public" }]
                })
        ] : []),
        optimization: {
            splitChunks: {
                chunks: 'all'
            },
        },
    },
    {
        entry: "./src/Solver.fs",
        output: {
            path: isProduction ? path.join(__dirname, "./deploy/Workers") : path.join(__dirname, './public/Workers'),
            filename: "Solver.js",
            library: "Solver",
            libraryTarget: "umd",
        },
        mode: isProduction ? 'production' : 'development',
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        resolve: {
            symlinks: false
        },
        module: {
            rules: [
                {
                    test: /\.fs(x|proj)?$/,
                    use: {
                        loader: 'fable-loader',
                        options: {
                            allFiles: true,
                            define: [],
                            silent: true,
                        }
                    }
                },
                {
                    test: /\.js$/,
                    exclude: /node_modules/,
                    use: {
                        loader: 'babel-loader'
                    }
                }
            ]
        },
        target: 'webworker'
    }
]


function hasArg(arg) {
    return arg instanceof RegExp
        ? process.argv.some(x => arg.test(x))
        : process.argv.indexOf(arg) !== -1;
}