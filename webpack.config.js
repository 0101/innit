// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");

module.exports = [
    {
        mode: "development",
        entry: "./src/App.fsproj",
        output: {
            path: path.join(__dirname, "./public"),
            filename: "bundle.js",
        },
        devServer: {
            publicPath: "/",
            contentBase: "./public",
            port: 8080,
            host: 'localhost',
            allowedHosts: [
                'localhost',
                '0.0.0.0',
                '192.168.1.217'
            ]
        },
        module: {
            rules: [{
                test: /\.fs(x|proj)?$/,
                use: "fable-loader"
            }]
        }
    },
    {
        entry: "./src/Solver.fs",
        output: {
            path: path.join(__dirname, './public/Workers'),
            filename: "Solver.js",
            library: "Solver",
            libraryTarget: "umd",
        },
        mode: "development",
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
                            define: []
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